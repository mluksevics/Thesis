using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Terminations;
using GlassAssistant.WindowMain;
using System;
using System.Linq;

namespace GlassAssistant.Optimization
{
    internal class ControllerGlassOpt : ControllerBaseGenetic
    {
        #region Fields

        private FitnessGlassUnit fitness;
        private readonly FormInputData inputData;
        private readonly SettingsOpt settings;

        #endregion Fields

        #region Constructors

        public ControllerGlassOpt(FormInputData input, SettingsOpt settings)
        {
            this.inputData = input;
            this.settings = settings;
        }

        #endregion Constructors

        #region Methods

        public override IFitness CreateFitness()
        {
            this.fitness = new FitnessGlassUnit(this.inputData, this.settings);

            return this.fitness;
        }

        public override IChromosome CreateChromosome()
        {
            return new ChromosomeGlass(ConstantsOpt.NumberOfGenes);
        }

        public override ITermination CreateTermination()
        {
            var terminations = new TimeEvolvingTermination(TimeSpan.FromSeconds(this.settings.MaxOptimizationTimeSeconds));
            var fitnessStagnationTermination = new FitnessStagnationTermination(this.settings.MaxOptimizationStagnatingGenerations);
            return new OrTermination(terminations, fitnessStagnationTermination);
        }

        /// <summary>
        /// Displays the sample.
        /// </summary>
        /// <param name="bestChromosome">The current best chromosome</param>
        public override void Draw(IChromosome bestChromosome)
        {
            var c = bestChromosome as ChromosomeGlass;
            var genesStringArray = bestChromosome.GetGenes().Select(g => g.Value.ToString()).ToArray();
        }

        #endregion Methods
    }
}