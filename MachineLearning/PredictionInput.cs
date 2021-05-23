using GlassAssistant.Constants;
using GlassAssistant.Enums;
using Newtonsoft.Json.Linq;
using RFEMCommon.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GlassAssistant.MachineLearning
{
    internal class PredictionInput
    {
        public int SurfaceNo { get; set; }
        public int CombinationNo { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Thickness { get; set; }
        public double UniformLoadMagnitude { get; set; }
        public double PointLoadMagnitude { get; set; }
        public double LineLoadHeight { get; set; }
        public double LineLoadMagnitude { get; set; }
        public int LinePointDirection { get; set; }
        public LimitState LimitStateForChecks { get; set; }

        public JObject GetSingleJsonObject()
        {
            var inputObject = new JObject
            (
                new JProperty("height", Height),
                new JProperty("line_height", LineLoadHeight),
                new JProperty("line_magnit", LineLoadMagnitude),
                new JProperty("line_pt_dir", LinePointDirection),
                new JProperty("pt_magnitude", PointLoadMagnitude),
                new JProperty("thickness", Thickness),
                new JProperty("udl_magnitude", UniformLoadMagnitude),
                new JProperty("width", Width)
            );
            return inputObject;
        }

        private static JObject GetPredictionsObjectForSending(IEnumerable<PredictionInput> predictionObjectList)
        {
            return new JObject(new JProperty("instances", predictionObjectList.Select(x => x.GetSingleJsonObject())));
        }

        public static async Task<List<ResultsEnvelope>> GetDeflectionPredictions(List<PredictionInput> predictionInputList)
        {
            var predictionJson = PredictionInput.GetPredictionsObjectForSending(predictionInputList);
            var deflectionPredictionList = GetPredictionsFromServer(predictionJson, MachineLearningConstants.PredictionServerDeflection);

            var resultsEnvelopesList = new List<ResultsEnvelope>();
            for (int i = 0; i < predictionInputList.Count; i++)
            {
                if (predictionInputList[i].UniformLoadMagnitude > MachineLearningConstants.MaxUdlLoadAllowedInMachineLearning)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Udl larger than allowed for machine learning model, actual {predictionInputList[i].UniformLoadMagnitude}");
                }
                var envelope = new ResultsEnvelope()
                {
                    TotalDisplacement = deflectionPredictionList.predictions[i] * Constants.Conversion.MmToM,
                    TotalDisplacementEnvelopeCase = predictionInputList[i].CombinationNo,
                    TotalDisplacementEnvelopeElement = predictionInputList[i].SurfaceNo,
                };
                resultsEnvelopesList.Add(envelope);
            }

            return resultsEnvelopesList;
        }

        public static async Task<List<ResultsEnvelope>> GetStressPredictions(List<PredictionInput> predictionInputList)
        {
            var predictionJson = PredictionInput.GetPredictionsObjectForSending(predictionInputList);
            var stressPredictionList = GetPredictionsFromServer(predictionJson, MachineLearningConstants.PredictionServerStress);

            var resultsEnvelopesList = new List<ResultsEnvelope>();
            for (int i = 0; i < predictionInputList.Count; i++)
            {
                var envelope = new ResultsEnvelope()
                {
                    Stress = stressPredictionList.predictions[i] * Constants.Conversion.MpaToPa,
                    StressEnvelopeCase = predictionInputList[i].CombinationNo,
                    StressEnvelopeElement = predictionInputList[i].SurfaceNo,
                };
                resultsEnvelopesList.Add(envelope);
            }

            return resultsEnvelopesList;
        }

        private static PredictionPlainResult GetPredictionsFromServer(JObject predictionJson, string serverAddress)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5.0);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", MachineLearningConstants.AccessToken);
            var content = new StringContent(predictionJson.ToString(), Encoding.UTF8, "application/json");

            string response;
            try
            {
                response = client.PostAsync(serverAddress, content).Result.Content.ReadAsStringAsync().Result;
            }
            catch (Exception exception)
            {
                throw new HttpRequestException("Error when connecting to machine learning server. " +
                                               "Check your connection to server and VPN.", exception);
            }
            var predictionResult = ParseHttpResponse(response);
            return predictionResult;
        }

        private static PredictionPlainResult ParseHttpResponse(string response)
        {
            var responseJson = JToken.Parse(response);
            var predictionResult = JObject.Parse(responseJson.ToString()).ToObject<PredictionPlainResult>();
            return predictionResult;
        }
    }
}