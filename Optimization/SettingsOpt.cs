namespace GlassAssistant.Optimization
{
    public class SettingsOpt
    {
        public double MaxOptimizationTimeSeconds { get; set; }
        public int MaxOptimizationStagnatingGenerations { get; set; }
        public double MaxAllowedDeflection { get; set; }
        public double MaxAllowedStressRatio { get; set; }
        public double MinCavityThickness { get; set; }
        public double MaxCavityThickness { get; set; }
        public bool IsExternalPlaneStifferThanInternal { get; set; }
        public bool IsSymmetricLaminateUsed { get; set; }
        public int MinPopulationSize { get; set; }
        public int MaxPopulationSize { get; set; }

        public GlassPaneTypeConsideredInOptimization ExternalLaminateOrMonolithic { get; set; }
        public GlassPaneTypeConsideredInOptimization InternalLaminateOrMonolithic { get; set; }
        public GlassPaneTypeConsideredInOptimization MiddleLaminateOrMonolithic { get; set; }
        public GlassPaneGradeConsideredInOptimization ExternalGradeAllowed { get; set; }
        public GlassPaneGradeConsideredInOptimization InternalGradeAllowed { get; set; }
        public GlassPaneGradeConsideredInOptimization MiddleGradeAllowed { get; set; }
    }
}