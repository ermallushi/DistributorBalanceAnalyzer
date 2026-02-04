using Microsoft.ML.Data;

namespace DistributorBalanceAnalyzer.ML.Models
{
    public class DailySpendingData
    {
        public DateTime Date { get; set; }
        public float DailyDebits { get; set; }
        public float TransactionCount { get; set; }
        public float AverageTransactionSize { get; set; }
        public int DayOfWeek { get; set; }
    }

    public class AnomalyPrediction
    {
        // ML.NET outputs prediction result to 'PredictedLabel' column
        // which we map to a more descriptive property name
        [ColumnName("PredictedLabel")]
        public bool IsAnomaly { get; set; }
        
        [ColumnName("Score")]
        public float AnomalyScore { get; set; }
    }
}
