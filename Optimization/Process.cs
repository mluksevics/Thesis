using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Populations;
using GlassAssistant.WindowMain;
using GlassAssistant.WindowOptimize;
using System;
using System.Threading;

namespace GlassAssistant.Optimization
{
    public static class Process
    {
        public static FormInputData Run(FormInputData inputData, SettingsOpt settings,
            OptimizeWindowViewModel viewModel, CancellationToken token)
        {
            var optController = GetOptimizationController(inputData, settings);
            var geneticAlgorithm = SetGeneticAlgorithmSettings(settings, optController);

            geneticAlgorithm.GenerationRan += delegate
            {
                OutputGenerationResults(viewModel, geneticAlgorithm, inputData);
                if (token.IsCancellationRequested)
                {
                    geneticAlgorithm.Stop();
                    LogLine(viewModel, "Optimization stopped by user!");
                }
            };

            var optimizedInputData = RunOptimization(inputData, viewModel, geneticAlgorithm);
            return optimizedInputData;
        }

        private static ControllerGlassOpt GetOptimizationController(FormInputData inputData, SettingsOpt settings)
        {
            var optController = new ControllerGlassOpt(inputData, settings);
            optController.Initialize();
            return optController;
        }

        private static GeneticAlgorithm SetGeneticAlgorithmSettings(SettingsOpt settings, ControllerGlassOpt optController)
        {
            var selection = optController.CreateSelection();
            var crossover = optController.CreateCrossover();
            var mutation = optController.CreateMutation();
            var fitness = optController.CreateFitness();
            var adamChromosome = optController.CreateChromosome();
            var population = new Population(settings.MinPopulationSize, settings.MaxPopulationSize,
                adamChromosome);
            population.GenerationStrategy = new TrackingGenerationStrategy();

            var geneticAlgorithm = new GeneticAlgorithm(population, fitness, selection, crossover, mutation);
            optController.ConfigGA(geneticAlgorithm);

            geneticAlgorithm.Termination = optController.CreateTermination();
            geneticAlgorithm.MutationProbability = ConstantsOpt.MutationRate;
            geneticAlgorithm.CrossoverProbability = ConstantsOpt.CrossoverRate;

            return geneticAlgorithm;
        }

        private static FormInputData RunOptimization(FormInputData inputData, OptimizeWindowViewModel viewModel,
            GeneticAlgorithm ga)
        {
            try
            {
                LogLine(viewModel, "Optimization Started!");
                ga.Start();
                LogLine(viewModel, "Optimization stopped!");
                FitnessGlassUnit.UpdateInputDataFromChromosome((ChromosomeGlass)ga.BestChromosome, inputData);
                return inputData;
            }
            catch (Exception ex)
            {
                LogLine(viewModel, $"Error: {ex.Message}");
                return inputData;
            }
        }

        private static void OutputGenerationResults(OptimizeWindowViewModel viewModel, GeneticAlgorithm ga,
            FormInputData inputData)
        {
            var bestChromosome = (ChromosomeGlass)ga.Population.BestChromosome;
            var bestThickness = GetBestThicknessMm(bestChromosome);
            FitnessGlassUnit.UpdateInputDataFromChromosome(bestChromosome, inputData);
            LogLine(viewModel, $"Generation: {ga.Population.GenerationsNumber} | Total thickness: {bestThickness}");
            LogLine(viewModel, $"Time: {ga.TimeEvolving:mm\\:ss\\.f} | Buildup: {FormInputData.GetBuildupDescriptionString(inputData)}");
        }

        private static string GetBestThicknessMm(IChromosome bestChromosome)
        {
            return (ConstantsOpt.FitnessMaxValue - bestChromosome.Fitness) * Constants.Conversion.MtoMm + "mm";
        }

        private static void LogLine(OptimizeWindowViewModel viewModel, string text)
        {
            viewModel.LogLines.AddOnUI(text);
        }
    }
}