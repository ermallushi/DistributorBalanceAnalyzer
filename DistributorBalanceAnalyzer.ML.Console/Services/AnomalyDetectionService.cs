using Microsoft.ML;
using DistributorBalanceAnalyzer.ML.Models;

namespace DistributorBalanceAnalyzer.ML.Services
{
    public class AnomalyDetectionService
    {
        private readonly MLContext _mlContext;

        public AnomalyDetectionService()
        {
            _mlContext = new MLContext(seed: 1);
        }

        public List<(DateTime Date, bool IsAnomaly, float Score)> DetectSpendingAnomalies(List<DailySpendingData> dailySpending)
        {
            if (dailySpending.Count < 30)
            {
                System.Console.WriteLine("⚠️  Need at least 30 days of data for anomaly detection");
                return new List<(DateTime, bool, float)>();
            }

            var dataView = _mlContext.Data.LoadFromEnumerable(dailySpending);

            var pipeline = _mlContext.Transforms.DetectSpikeBySsa(
                outputColumnName: nameof(AnomalyPrediction.IsAnomaly),
                inputColumnName: nameof(DailySpendingData.DailyDebits),
                confidence: 95.0,
                pvalueHistoryLength: dailySpending.Count / 4,
                trainingWindowSize: dailySpending.Count / 2,
                seasonalityWindowSize: dailySpending.Count / 2);

            var model = pipeline.Fit(dataView);
            var transformedData = model.Transform(dataView);

            var predictions = _mlContext.Data.CreateEnumerable<AnomalyPrediction>(transformedData, reuseRowObject: false).ToList();

            var results = new List<(DateTime, bool, float)>();
            for (int i = 0; i < dailySpending.Count; i++)
            {
                results.Add((dailySpending[i].Date, predictions[i].IsAnomaly, predictions[i].AnomalyScore));
            }

            return results;
        }
    }
}
