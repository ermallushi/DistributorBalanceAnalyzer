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
        [ColumnName("PredictedLabel")]
        public bool IsAnomaly { get; set; }
        
        [ColumnName("Score")]
        public float AnomalyScore { get; set; }
    }
}
