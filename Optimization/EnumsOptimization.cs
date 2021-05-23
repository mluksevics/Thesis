using GlassAssistant.Exceptions;

namespace GlassAssistant.Optimization
{
    public enum GlassThickness
    {
        Thk4mm,
        Thk5mm,
        Thk6mm,
        Thk8mm,
        Thk10mm,
        Thk12mm
    };

    public enum CavityThickness
    {
        Thk8mm,
        Thk10mm,
        Thk12mm,
        Thk14mm,
        Thk15mm,
        Thk16mm,
        Thk18mm,
        Thk20mm
    };

    public enum GlassPaneTypeConsideredInOptimization
    {
        Monolithic,
        Laminated,
        Any
    };

    public enum GlassPaneGradeConsideredInOptimization
    {
        FloatOnly,
        FloatOrHeatStrengthened,
        Any
    };

    public enum GlassGradeSuitableForPane
    {
        Float,
        Hs,
        Ht,
        None
    };

    public static class GlassGradeConsideredInOptimizationExtensions
    {
        public static bool CheckActualGrade(this GlassPaneGradeConsideredInOptimization gradeAllowed, GlassGradeSuitableForPane actualGradeSuitable)
        {
            if (actualGradeSuitable == GlassGradeSuitableForPane.None)
            {
                return false;
            }

            switch (gradeAllowed)
            {
                case GlassPaneGradeConsideredInOptimization.Any:
                    return true;

                case GlassPaneGradeConsideredInOptimization.FloatOrHeatStrengthened:
                    return (actualGradeSuitable == GlassGradeSuitableForPane.Float ||
                        actualGradeSuitable == GlassGradeSuitableForPane.Hs);

                case GlassPaneGradeConsideredInOptimization.FloatOnly:
                    return (actualGradeSuitable == GlassGradeSuitableForPane.Float);

                default:
                    throw new EnumValueOutOfRangeException($"Unknown setting {gradeAllowed} for allowed glass plane grade!");
            }
        }
    }

    public static class GlassThicknessExtensions
    {
        public static double GetThicknessInMeters(this GlassThickness glassThickness)
        {
            switch (glassThickness)
            {
                case GlassThickness.Thk4mm:
                    return 0.004;

                case GlassThickness.Thk5mm:
                    return 0.005;

                case GlassThickness.Thk6mm:
                    return 0.006;

                case GlassThickness.Thk8mm:
                    return 0.008;

                case GlassThickness.Thk10mm:
                    return 0.010;

                case GlassThickness.Thk12mm:
                    return 0.012;

                default:
                    throw new EnumValueOutOfRangeException($"Unknown type of optimization glass thickness: {glassThickness}");
            }
        }

        public static double GetCavityInMeters(this CavityThickness cavityThickness)
        {
            switch (cavityThickness)
            {
                case CavityThickness.Thk8mm:
                    return 0.008;

                case CavityThickness.Thk10mm:
                    return 0.010;

                case CavityThickness.Thk12mm:
                    return 0.012;

                case CavityThickness.Thk14mm:
                    return 0.014;

                case CavityThickness.Thk15mm:
                    return 0.015;

                case CavityThickness.Thk16mm:
                    return 0.016;

                case CavityThickness.Thk18mm:
                    return 0.018;

                case CavityThickness.Thk20mm:
                    return 0.020;

                default:
                    throw new EnumValueOutOfRangeException($"Unknown type of optimisation glass thickness: {cavityThickness}");
            }
        }
    }
}