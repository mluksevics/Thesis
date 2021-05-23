using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using GlassAssistant.Constants;
using GlassAssistant.DataStructures;
using GlassAssistant.Enums;
using GlassAssistant.Exceptions;
using GlassAssistant.WindowMain;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;

namespace GlassAssistant.Optimization
{
    public class FitnessGlassUnit : IFitness
    {
        #region Fields

        private readonly FormInputData inputData;
        private readonly SettingsOpt settings;

        #endregion Fields

        #region Constructor

        public FitnessGlassUnit(FormInputData input, SettingsOpt settings)
        {
            this.inputData = input;
            this.settings = settings;
        }

        #endregion Constructor

        #region Methods For Interface implementation

        public double Evaluate(IChromosome chromosome)
        {
            chromosome = ModifyChomosomeAccordingToSettings((ChromosomeGlass)chromosome, this.settings);
            var updatedInputFormData = UpdateInputDataFromChromosome((ChromosomeGlass)chromosome, this.inputData);
            if (!IsInputDataInAllowedRange(updatedInputFormData))
            {
                return ConstantsOpt.FitnessIfChecksFail;
            }

            var results = MachineLearning.Process.FullPredictionProcess(updatedInputFormData).Result;

            var maxDeflectionResult = results.DeflectionResultsList.First().DeflectionValue * Conversion.MmToM; // Convert to [m]
            var isStressResultsAcceptable = IsStressResultsAcceptable(results.StressResultsList);
            if (maxDeflectionResult > this.settings.MaxAllowedDeflection ||
                !isStressResultsAcceptable)
            {
                return ConstantsOpt.FitnessIfChecksFail;
            }

            ((ChromosomeGlass)chromosome).DeflectionResults = results.DeflectionResultsList.ToList();
            ((ChromosomeGlass)chromosome).StressResults = results.StressResultsList.ToList();

            var totalThickness = CalculateTotalGlassThickness(updatedInputFormData);
            return (ConstantsOpt.FitnessMaxValue - totalThickness);
        }

        #endregion Methods For Interface implementation

        #region Methods

        public static FormInputData UpdateInputDataFromChromosome(ChromosomeGlass chromosome, FormInputData inputData)
        {
            var updatedInput = inputData;

            var genes = chromosome.GetGenes();
            updatedInput.ExternalLayer1Thickness = ((GlassThickness)genes[ConstantsOpt.ExtThk1GeneNo].Value).GetThicknessInMeters();
            updatedInput.ExternalLayer2Thickness = ((GlassThickness)genes[ConstantsOpt.ExtThk2GeneNo].Value).GetThicknessInMeters();
            updatedInput.MiddleLayer1Thickness = ((GlassThickness)genes[ConstantsOpt.MiddleThk1GeneNo].Value).GetThicknessInMeters();
            updatedInput.MiddleLayer2Thickness = ((GlassThickness)genes[ConstantsOpt.MiddleThk2GeneNo].Value).GetThicknessInMeters();
            updatedInput.InternalLayer1Thickness = ((GlassThickness)genes[ConstantsOpt.IntThk1GeneNo].Value).GetThicknessInMeters();
            updatedInput.InternalLayer2Thickness = ((GlassThickness)genes[ConstantsOpt.IntThk2GeneNo].Value).GetThicknessInMeters();
            updatedInput.ExternalIsMonolithic = ((GlassPaneType)genes[ConstantsOpt.ExtMonolithGeneNo].Value) == GlassPaneType.Monolithic;
            updatedInput.InternalIsMonolithic = ((GlassPaneType)genes[ConstantsOpt.IntMonolithGeneNo].Value) == GlassPaneType.Monolithic;
            updatedInput.MiddleIsMonolithic = ((GlassPaneType)genes[ConstantsOpt.MiddleMonolithGeneNo].Value) == GlassPaneType.Monolithic;
            updatedInput.Cavity1Thickness = ((CavityThickness)genes[ConstantsOpt.Cavity1GeneNo].Value).GetCavityInMeters();
            updatedInput.Cavity2Thickness = ((CavityThickness)genes[ConstantsOpt.Cavity2GeneNo].Value).GetCavityInMeters();

            return updatedInput;
        }

        private bool IsInputDataInAllowedRange(FormInputData inputDataFromChromosome)
        {
            var isCavitiesAcceptable = IsTotalCavitySizeWithinBounds(inputDataFromChromosome);
            var isExternalPaneStifferThanInternal = IsExternalPaneStifferThanInternal(inputDataFromChromosome);

            if (this.settings.IsExternalPlaneStifferThanInternal && !isExternalPaneStifferThanInternal)
            {
                return false;
            }

            if (!isCavitiesAcceptable)
            {
                return false;
            }

            return true;
        }

        private bool IsExternalPaneStifferThanInternal(FormInputData inputDataFromChromosome)
        {
            var externalThickness = inputDataFromChromosome.ExternalIsMonolithic
                ? inputDataFromChromosome.ExternalLayer1Thickness
                : inputDataFromChromosome.ExternalLayer1Thickness + inputDataFromChromosome.ExternalLayer2Thickness;

            var internalThickness = inputDataFromChromosome.InternalIsMonolithic
                ? inputDataFromChromosome.InternalLayer1Thickness
                : inputDataFromChromosome.InternalLayer1Thickness + inputDataFromChromosome.InternalLayer2Thickness;

            if (externalThickness > internalThickness ||
                (externalThickness == internalThickness && (
                inputDataFromChromosome.ExternalIsMonolithic &&
                !inputDataFromChromosome.InternalIsMonolithic)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsTotalCavitySizeWithinBounds(FormInputData inputDataFromChromosome)
        {
            var cavity1 = inputDataFromChromosome.Cavity1Thickness;
            var cavity2 = inputDataFromChromosome.Cavity2Thickness;
            if (inputDataFromChromosome.UnitType == Enums.GlassUnitType.Triple)
            {
                if (cavity1 + cavity2 > this.settings.MaxCavityThickness ||
                    cavity1 + cavity2 < this.settings.MinCavityThickness)
                {
                    return false;
                }
            }
            else
            {
                if (cavity1 > this.settings.MaxCavityThickness ||
                    cavity1 < this.settings.MinCavityThickness)
                {
                    return false;
                }
            }

            return true;
        }

        private GlassGradeSuitableForPane GetRequiredGlassGradeForGlassPane(IEnumerable<OutputOfStressResultsTable> glassPaneStressResults)
        {
            var allowedUtilizationRatio = this.settings.MaxAllowedStressRatio;
            var floatUtilization = glassPaneStressResults.MaxBy(x => x.UtilisationFloat).First().UtilisationFloat;
            if (floatUtilization <= allowedUtilizationRatio)
            {
                return GlassGradeSuitableForPane.Float;
            }

            var HsUtilization = glassPaneStressResults.MaxBy(x => x.UtilisationHs).First().UtilisationHs;
            if (HsUtilization <= allowedUtilizationRatio)
            {
                return GlassGradeSuitableForPane.Hs;
            }

            var HtUtilization = glassPaneStressResults.MaxBy(x => x.UtilisationHt).First().UtilisationHt;
            if (HtUtilization <= allowedUtilizationRatio)
            {
                return GlassGradeSuitableForPane.Ht;
            }

            return GlassGradeSuitableForPane.None;
        }

        private GlassGradeSuitableForPane CheckPaneExistsAndGetSuitableGrade(IEnumerable<OutputOfStressResultsTable> stressResultsList, GlassPaneLocation glassPaneLocation)
        {
            var resultsForGlassPane = stressResultsList.Where(x => x.Pane == glassPaneLocation.GetSurfaceDescription());
            if (resultsForGlassPane.Any())
            {
                return GetRequiredGlassGradeForGlassPane(resultsForGlassPane);
            }
            else
            {
                return GlassGradeSuitableForPane.None;
            }
        }

        private bool IsStressResultsAcceptable(IEnumerable<OutputOfStressResultsTable> stressResultsList)
        {
            var externalPaneSuitableGrade = CheckPaneExistsAndGetSuitableGrade(stressResultsList, GlassPaneLocation.External);

            var middlePaneSuitableGrade = CheckPaneExistsAndGetSuitableGrade(stressResultsList, GlassPaneLocation.Middle);

            var internalPaneSuitableGrade = CheckPaneExistsAndGetSuitableGrade(stressResultsList, GlassPaneLocation.Internal);

            bool externalPaneCheck, internalPaneCheck, middlePaneCheck;
            switch (inputData.UnitType)
            {
                case GlassUnitType.Single:
                case GlassUnitType.Balustrade:
                    return this.settings.ExternalGradeAllowed.CheckActualGrade(externalPaneSuitableGrade);

                case GlassUnitType.Double:
                    externalPaneCheck = this.settings.ExternalGradeAllowed.CheckActualGrade(externalPaneSuitableGrade);
                    internalPaneCheck = this.settings.InternalGradeAllowed.CheckActualGrade(internalPaneSuitableGrade);
                    return (externalPaneCheck && internalPaneCheck);

                case GlassUnitType.Triple:
                    externalPaneCheck = this.settings.ExternalGradeAllowed.CheckActualGrade(externalPaneSuitableGrade);
                    internalPaneCheck = this.settings.InternalGradeAllowed.CheckActualGrade(internalPaneSuitableGrade);
                    middlePaneCheck = this.settings.MiddleGradeAllowed.CheckActualGrade(middlePaneSuitableGrade);
                    return (externalPaneCheck && internalPaneCheck && middlePaneCheck);

                default:
                    throw new EnumValueOutOfRangeException($"Unknown glass unit type {inputData.UnitType} when processing mahine learning results!");
            }
        }

        private static double CalculateTotalGlassThickness(FormInputData inputData)
        {
            double totalThickness = 0;

            if (inputData.ExternalIsMonolithic)
            {
                totalThickness += inputData.ExternalLayer1Thickness;
            }
            else
            {
                totalThickness += inputData.ExternalLayer1Thickness;
                totalThickness += inputData.ExternalLayer2Thickness;
            }

            if (inputData.UnitType != GlassUnitType.Single &&
                inputData.UnitType != GlassUnitType.Balustrade)
            {
                if (inputData.InternalIsMonolithic)
                {
                    totalThickness += inputData.InternalLayer1Thickness;
                }
                else
                {
                    totalThickness += inputData.InternalLayer1Thickness;
                    totalThickness += inputData.InternalLayer2Thickness;
                }
            }
            if (inputData.UnitType == GlassUnitType.Triple)
            {
                if (inputData.MiddleIsMonolithic)
                {
                    totalThickness += inputData.MiddleLayer1Thickness;
                }
                else
                {
                    totalThickness += inputData.MiddleLayer1Thickness;
                    totalThickness += inputData.MiddleLayer2Thickness;
                }
            }

            return totalThickness;
        }

        private static IChromosome ModifyChomosomeAccordingToSettings(ChromosomeGlass chromosome, SettingsOpt settings)
        {
            //
            if (settings.IsSymmetricLaminateUsed)
            {
                chromosome.ReplaceGene(ConstantsOpt.ExtThk1GeneNo, chromosome.GetGene(ConstantsOpt.ExtThk2GeneNo));
                chromosome.ReplaceGene(ConstantsOpt.IntThk1GeneNo, chromosome.GetGene(ConstantsOpt.IntThk2GeneNo));
                chromosome.ReplaceGene(ConstantsOpt.MiddleThk1GeneNo, chromosome.GetGene(ConstantsOpt.MiddleThk2GeneNo));
            }

            // Set monolith
            if (settings.ExternalLaminateOrMonolithic == GlassPaneTypeConsideredInOptimization.Monolithic)
            {
                chromosome.ReplaceGene(ConstantsOpt.ExtMonolithGeneNo, new Gene(GlassPaneType.Monolithic));
            }

            if (settings.InternalLaminateOrMonolithic == GlassPaneTypeConsideredInOptimization.Monolithic)
            {
                chromosome.ReplaceGene(ConstantsOpt.IntMonolithGeneNo, new Gene(GlassPaneType.Monolithic));
            }

            if (settings.MiddleLaminateOrMonolithic == GlassPaneTypeConsideredInOptimization.Monolithic)
            {
                chromosome.ReplaceGene(ConstantsOpt.MiddleMonolithGeneNo, new Gene(GlassPaneType.Monolithic));
            }

            // Set laminate
            if (settings.ExternalLaminateOrMonolithic == GlassPaneTypeConsideredInOptimization.Laminated)
            {
                chromosome.ReplaceGene(ConstantsOpt.ExtMonolithGeneNo, new Gene(GlassPaneType.Laminated));
            }

            if (settings.InternalLaminateOrMonolithic == GlassPaneTypeConsideredInOptimization.Laminated)
            {
                chromosome.ReplaceGene(ConstantsOpt.IntMonolithGeneNo, new Gene(GlassPaneType.Laminated));
            }

            if (settings.MiddleLaminateOrMonolithic == GlassPaneTypeConsideredInOptimization.Laminated)
            {
                chromosome.ReplaceGene(ConstantsOpt.MiddleMonolithGeneNo, new Gene(GlassPaneType.Laminated));
            }

            return chromosome;
        }

        #endregion Methods
    }
}