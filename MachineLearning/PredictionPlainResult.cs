using System.Collections.Generic;

namespace GlassAssistant.MachineLearning
{
    public class PredictionPlainResult
    {
        /// <summary>
        /// Naming convention not followed because this class is used to
        /// deserialize Json received from Tensorflow machine learning servers.
        /// "prediction" is the only property of the class sent by Tensorflow.
        /// </summary>
        public List<double> predictions { get; set; }
    }
}