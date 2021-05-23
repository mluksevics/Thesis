namespace GlassAssistant.Optimization
{
    public static class ConstantsOpt
    {
        public const int DefaultMaxRunTimeSeconds = 120;
        public const int DefaultMaxRunTimeNoImprovementsIterations = 10;
        public const int DefaultMaxAllowedDeflectionRatio = 100;

        public const float MutationRate = 0.1f;
        public const float CrossoverRate = 0.80f;

        public const double FitnessMaxValue = 1.0;

        public const int NumberOfGenes = 11;
        public const double FitnessIfChecksFail = 0;

        public const int ExtThk1GeneNo = 0;
        public const int ExtThk2GeneNo = 1;
        public const int ExtMonolithGeneNo = 2;

        public const int MiddleThk1GeneNo = 3;
        public const int MiddleThk2GeneNo = 4;
        public const int MiddleMonolithGeneNo = 5;

        public const int IntThk1GeneNo = 6;
        public const int IntThk2GeneNo = 7;
        public const int IntMonolithGeneNo = 8;

        public const int Cavity1GeneNo = 9;
        public const int Cavity2GeneNo = 10;
    }
}