using GlassAssistant.Constants;
using GlassAssistant.DataProcessing;
using GlassAssistant.DataStructures;
using GlassAssistant.Enums;
using GlassAssistant.WindowMain;
using MoreLinq;
using RFEMCommon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GlassAssistant.MachineLearning
{
    public static class Process
    {
        public static async Task<FormResultsData> FullPredictionProcess(FormInputData inputFormData)
        {
            var validationErrors = InputDataValidation.MachineLearningSpecificValidation(inputFormData);
            if (validationErrors.Any())
            {
                InputDataValidation.ThrowValidationErrors(validationErrors);
                return null;
            }

            var glassUnitList = GlassUnit.CreateGlassUnitsWithAllInterlayerStiffness(inputFormData).ToList();
            var loadCombinations = inputFormData.LoadCombinations;

            var loadSettings = new LoadSettings(inputFormData);
            var externalLoads =
                LoadGenerationExternal.GenerateExternalLoadsAllUnits(loadSettings, glassUnitList, new ObjectsStore(), true);
            var climateLoads = LoadGenerationClimate.GenerateClimaticLoadsAllUnits(loadSettings, glassUnitList);

            glassUnitList
                .Where(x => x.IsUnitWithCavity)
                .ForEach(x => x.SetDeflectedVolumeEmpiricCalculation(externalLoads));
            var transferredLoads = LoadTransferThroughCavity.GenerateTransferredLoads(glassUnitList);

            var allLoads = externalLoads.Merge(climateLoads).Merge(transferredLoads);
            var machineLearningObjects = CreateAllPredictionObjects(glassUnitList, loadCombinations, allLoads);
            var resultsFormData = await GetPredictionsAndResultsOutput(inputFormData, machineLearningObjects, glassUnitList);

            return resultsFormData;
        }

        private static async Task<FormResultsData> GetPredictionsAndResultsOutput(FormInputData inputFormData, List<PredictionInput> machineLearningObjects,
            List<GlassUnit> glassUnitList)
        {
            var deflectionPredictionInput = machineLearningObjects.Where(x => x.LimitStateForChecks == LimitState.SLS);
            var stressPredictionInput = machineLearningObjects.Where(x => x.LimitStateForChecks == LimitState.ULS);

            var deflectionResultTask = PredictionInput.GetDeflectionPredictions(deflectionPredictionInput.ToList());
            var stressResultTask = PredictionInput.GetStressPredictions(stressPredictionInput.ToList());
            var taskList = new List<Task>() { deflectionResultTask, stressResultTask };
            await Task.WhenAll(taskList);

            var results = new Results(inputFormData, glassUnitList, CalculationType.MachineLearning);
            var resultsFormData = results.GetOutputOfResults(deflectionResultTask.Result, stressResultTask.Result);
            return resultsFormData;
        }

        private static List<PredictionInput> CreateAllPredictionObjects(IEnumerable<GlassUnit> glassUnitList,
            ObservableCollection<LoadCombinationDefinition> loadCombinations,
            RfemLoadsData allLoads)
        {
            var allPredictionObjects = new List<PredictionInput>();

            foreach (var glassUnit in glassUnitList)
            {
                glassUnit.GlassPanes
                    .ForEach(glassPane => loadCombinations
                    .ForEach(comb => CreateSinglePredictionData(allLoads, comb, glassPane, allPredictionObjects)));
            }

            return allPredictionObjects;
        }

        private static void CreateSinglePredictionData(RfemLoadsData allLoads, LoadCombinationDefinition combination,
            GlassPane glassPane, List<PredictionInput> allPredObjects)
        {
            double totalUdlLoad = 0, pointLoadMagnitude = 0, lineLoadMagnitude = 0, lineLoadHeight = 0;
            int linePointDirection = 0;

            foreach (var loadCase in combination.LoadFactorDictionary)
            {
                totalUdlLoad = AddToTodalSurfaceLoad(allLoads, totalUdlLoad, glassPane, loadCase);
                lineLoadMagnitude = AddToTotalLineLoad(allLoads, loadCase, glassPane, lineLoadMagnitude, ref lineLoadHeight);
                pointLoadMagnitude = AddToTotalPointLoad(allLoads, loadCase, glassPane, pointLoadMagnitude);
            }

            if ((pointLoadMagnitude != 0 && pointLoadMagnitude * totalUdlLoad < 0) ||
                (lineLoadMagnitude != 0 && lineLoadMagnitude * totalUdlLoad < 0))
            {
                linePointDirection = -1;
            }
            else
            {
                linePointDirection = 1;
            }

            lineLoadHeight = InverseLineLoadHeightIfOverHalfOfTotalHeight(glassPane, lineLoadHeight);
            var predictionObject = new PredictionInput()
            {
                CombinationNo = combination.Number,
                SurfaceNo = glassPane.SurfaceNoRfem,
                LimitStateForChecks = combination.CheckType,
                Width = glassPane.Width,
                Height = glassPane.Height,
                Thickness = glassPane.EqThicknessForCorrectStiffness,
                UniformLoadMagnitude = Math.Abs(totalUdlLoad),
                LineLoadMagnitude = Math.Abs(lineLoadMagnitude),
                LineLoadHeight = lineLoadHeight,
                PointLoadMagnitude = Math.Abs(pointLoadMagnitude),
                LinePointDirection = linePointDirection
            };
            allPredObjects.Add(predictionObject);
        }

        private static double AddToTotalPointLoad(RfemLoadsData allLoads, KeyValuePair<LoadCaseInModel, double> loadCase, GlassPane glassPane,
            double pointLoadMagnitude)
        {
            var loadCaseNo = LoadCaseDefinition.Numbers[loadCase.Key];
            if (allLoads.RectangularLoadsByCase.ContainsKey(loadCaseNo))
            {
                var pointLoadUdl = allLoads.RectangularLoadsByCase[loadCaseNo]
                    .Where(x => glassPane.SurfaceNoRfem.ToString() == x.SurfaceList)
                    .Select(x => x.Magnitude1)
                    .DefaultIfEmpty(0)
                    .Sum() * loadCase.Value;
                var pointLoadSize = MachineLearningConstants.PointLoadSize;
                pointLoadMagnitude += pointLoadUdl * pointLoadSize * pointLoadSize;
            }

            return pointLoadMagnitude;
        }

        private static double AddToTotalLineLoad(RfemLoadsData allLoads, KeyValuePair<LoadCaseInModel, double> loadCase, GlassPane glassPane,
            double lineLoadMagnitude, ref double lineLoadHeight)
        {
            var loadCaseNo = LoadCaseDefinition.Numbers[loadCase.Key];
            if (allLoads.LineLoadsByCase.ContainsKey(loadCaseNo))
            {
                lineLoadMagnitude += allLoads.LineLoadsByCase[loadCaseNo]
                    .Where(x => glassPane.SurfaceNoRfem.ToString() == x.SurfaceList)
                    .Select(x => x.Magnitude1)
                    .DefaultIfEmpty(0)
                    .Sum() * loadCase.Value;
                lineLoadHeight = allLoads.LineLoadsByCase[loadCaseNo]
                    .Where(x => glassPane.SurfaceNoRfem.ToString() == x.SurfaceList)
                    .Select(x => x.Position1.Z)
                    .DefaultIfEmpty(0)
                    .FirstOrDefault();
            }

            return lineLoadMagnitude;
        }

        private static double AddToTodalSurfaceLoad(RfemLoadsData allLoads, double totalUdlLoad, GlassPane glassPane,
            KeyValuePair<LoadCaseInModel, double> loadCase)
        {
            var loadCaseNo = LoadCaseDefinition.Numbers[loadCase.Key];
            if (allLoads.SurfaceLoadsByCase.ContainsKey(loadCaseNo))
            {
                totalUdlLoad += allLoads.SurfaceLoadsByCase[loadCaseNo]
                    .Where(x => glassPane.SurfaceNoRfem.ToString() == x.SurfaceList)
                    .Select(x => x.Magnitude1)
                    .DefaultIfEmpty(0)
                    .Sum() * loadCase.Value;
            }

            return totalUdlLoad;
        }

        private static double InverseLineLoadHeightIfOverHalfOfTotalHeight(GlassPane glassPane, double lineLoadHeight)
        {
            lineLoadHeight = lineLoadHeight <= 0.5 * glassPane.Height
                ? lineLoadHeight
                : lineLoadHeight - 0.5 * glassPane.Height;
            return lineLoadHeight;
        }
    }
}