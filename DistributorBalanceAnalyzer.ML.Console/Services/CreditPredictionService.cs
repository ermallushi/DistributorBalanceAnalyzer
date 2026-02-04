using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using DistributorBalanceAnalyzer.ML.Models;

namespace DistributorBalanceAnalyzer.ML.Services
{
    public class CreditPredictionService
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;

        public CreditPredictionService()
        {
            _mlContext = new MLContext(seed: 1);
        }

        public void TrainModel(List<CreditTransactionData> historicalCredits)
        {
            var creditOnly = historicalCredits.OrderBy(c => c.Date).ToList();

            if (creditOnly.Count < 10)
            {
                System.Console.WriteLine("⚠️  Insufficient credit history for ML training (need at least 10 credits)");
                return;
            }

            var dataView = _mlContext.Data.LoadFromEnumerable(creditOnly);

            var pipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedCredits",
                inputColumnName: nameof(CreditTransactionData.Amount),
                windowSize: 7,
                seriesLength: creditOnly.Count,
                trainSize: creditOnly.Count,
                horizon: 5,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: "LowerBound",
                confidenceUpperBoundColumn: "UpperBound");

            _model = pipeline.Fit(dataView);
        }

        public CreditPrediction? PredictNextCredit(List<CreditTransactionData> historicalCredits, decimal currentBalance)
        {
            if (_model == null)
                TrainModel(historicalCredits);

            if (_model == null)
                return null;

            var forecastEngine = _model.CreateTimeSeriesEngine<CreditTransactionData, TimeSeriesPrediction>(_mlContext);
            var prediction = forecastEngine.Predict();

            var creditDates = historicalCredits.OrderBy(c => c.Date).Select(c => c.Date).ToList();
            var daysBetweenCredits = new List<int>();
            
            for (int i = 1; i < creditDates.Count; i++)
            {
                daysBetweenCredits.Add((creditDates[i] - creditDates[i - 1]).Days);
            }

            var avgDaysBetween = daysBetweenCredits.Any() ? (int)daysBetweenCredits.Average() : 30;
            var lastCreditDate = creditDates.LastOrDefault();
            var predictedDate = lastCreditDate.AddDays(avgDaysBetween);

            return new CreditPrediction
            {
                PredictedDate = predictedDate,
                PredictedAmount = prediction.ForecastedCredits[0],
                Confidence = Math.Max(0, Math.Min(1, 1.0f - Math.Abs(prediction.UpperBound[0] - prediction.LowerBound[0]) / Math.Max(prediction.ForecastedCredits[0], 1)))
            };
        }

        public List<CreditPrediction> PredictNext5Credits(List<CreditTransactionData> historicalCredits)
        {
            if (_model == null)
                TrainModel(historicalCredits);

            if (_model == null)
                return new List<CreditPrediction>();

            var forecastEngine = _model.CreateTimeSeriesEngine<CreditTransactionData, TimeSeriesPrediction>(_mlContext);
            var prediction = forecastEngine.Predict();

            var creditDates = historicalCredits.OrderBy(c => c.Date).Select(c => c.Date).ToList();
            var daysBetweenCredits = new List<int>();
            
            for (int i = 1; i < creditDates.Count; i++)
            {
                daysBetweenCredits.Add((creditDates[i] - creditDates[i - 1]).Days);
            }

            var avgDaysBetween = daysBetweenCredits.Any() ? (int)daysBetweenCredits.Average() : 30;
            var lastDate = creditDates.LastOrDefault();

            var predictions = new List<CreditPrediction>();
            for (int i = 0; i < Math.Min(5, prediction.ForecastedCredits.Length); i++)
            {
                // Confidence decreases exponentially with forecast distance
                var confidence = 0.95f * (float)Math.Pow(0.85, i);
                predictions.Add(new CreditPrediction
                {
                    PredictedDate = lastDate.AddDays(avgDaysBetween * (i + 1)),
                    PredictedAmount = prediction.ForecastedCredits[i],
                    Confidence = confidence
                });
            }

            return predictions;
        }
    }
}
