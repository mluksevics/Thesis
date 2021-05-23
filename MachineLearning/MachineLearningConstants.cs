namespace GlassAssistant.Constants
{
    public static class MachineLearningConstants
    {
        public const double PointLoadSize = 0.1;
        //public static readonly string PredictionServerDeflection = "http://139.59.184.184:8081/predict";
        //public static readonly string PredictionServerStress = "http://46.101.86.244:8081/predict";

        public static readonly string PredictionServerDeflection = "http://Aile-glass-ml-deformations.alto40.dev:8081/predict";
        public static readonly string PredictionServerStress = "http://Aile-glass-ml-stresses.alto40.dev:8081/predict";
        public static readonly string AccessToken = "";

        public const double MaxUdlLoadAllowedInMachineLearning = 25000; // [Pa]
    }
}