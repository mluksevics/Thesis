using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;
using GlassAssistant.DataStructures;
using GlassAssistant.Enums;
using System;
using System.Collections.Generic;

namespace GlassAssistant.Optimization
{
    public class ChromosomeGlass : ChromosomeBase
    {
        public List<OutputOfDeflectionResultsTable> DeflectionResults { get; set; }
        public List<OutputOfStressResultsTable> StressResults { get; set; }
        public double TotalThickness { get; set; }

        public ChromosomeGlass(int length) : base(length)
        {
            for (int i = 0; i < length; i++)
            {
                ReplaceGene(i, GenerateGene(i));
            }
        }

        public override IChromosome CreateNew()
        {
            return new ChromosomeGlass(ConstantsOpt.NumberOfGenes);
        }

        public override Gene GenerateGene(int geneIndex)
        {
            switch (geneIndex)
            {
                case ConstantsOpt.ExtThk1GeneNo:
                case ConstantsOpt.ExtThk2GeneNo:
                case ConstantsOpt.MiddleThk1GeneNo:
                case ConstantsOpt.MiddleThk2GeneNo:
                case ConstantsOpt.IntThk1GeneNo:
                case ConstantsOpt.IntThk2GeneNo:
                    return new Gene(GetRandomEnum<GlassThickness>());

                case ConstantsOpt.Cavity1GeneNo:
                case ConstantsOpt.Cavity2GeneNo:
                    return new Gene(GetRandomEnum<CavityThickness>());

                case ConstantsOpt.ExtMonolithGeneNo:
                case ConstantsOpt.MiddleMonolithGeneNo:
                case ConstantsOpt.IntMonolithGeneNo:
                    return new Gene(GetRandomEnum<GlassPaneType>());

                default:
                    throw new ArgumentOutOfRangeException($"Gene index {geneIndex} is out of known range in chomosome!");
            }
        }

        private static T GetRandomEnum<T>()
        {
            Array values = Enum.GetValues(typeof(T));
            var randomIndex = RandomizationProvider.Current.GetInt(0, values.Length);
            T randomEnumValue = (T)values.GetValue(randomIndex);

            return randomEnumValue;
        }
    }
}