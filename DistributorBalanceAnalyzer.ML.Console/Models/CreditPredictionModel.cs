using Microsoft.ML.Data;

namespace DistributorBalanceAnalyzer.ML.Models
{
    public class CreditTransactionData
    {
        public DateTime Date { get; set; }
        public float Amount { get; set; }
        public int DaysSinceLastCredit { get; set; }
        public int DayOfWeek { get; set; }
        public int DayOfMonth { get; set; }
        public int Month { get; set; }
        public float CurrentBalance { get; set; }
    }

    public class CreditPrediction
    {
        public DateTime PredictedDate { get; set; }
        public float PredictedAmount { get; set; }
        public float Confidence { get; set; }
    }

    public class TimeSeriesPrediction
    {
        [ColumnName("Score")]
        public float[] ForecastedCredits { get; set; } = Array.Empty<float>();
        public float[] LowerBound { get; set; } = Array.Empty<float>();
        public float[] UpperBound { get; set; } = Array.Empty<float>();
    }
}
