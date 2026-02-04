using DistributorBalanceAnalyzer.ML.Models;

namespace DistributorBalanceAnalyzer.ML.Services
{
    public class SmartDepletionService
    {
        private const int MaxSimulationDays = 365;
        private readonly CreditPredictionService _creditPredictor;

        public SmartDepletionService(CreditPredictionService creditPredictor)
        {
            _creditPredictor = creditPredictor;
        }

        public SmartDepletionForecast GenerateForecast(
            decimal currentBalance,
            decimal dailyBurnRate,
            DateTime mathematicalDepletionDate,
            List<CreditTransactionData> creditHistory)
        {
            var nextCreditPrediction = _creditPredictor.PredictNextCredit(creditHistory, currentBalance);

            if (nextCreditPrediction == null)
            {
                return new SmartDepletionForecast
                {
                    MathematicalDepletionDate = mathematicalDepletionDate,
                    AdjustedDepletionDate = mathematicalDepletionDate,
                    WillDeplete = true,
                    PredictedCredits = new List<CreditPrediction>(),
                    Message = "No ML predictions available - using mathematical calculation only"
                };
            }

            var currentDate = DateTime.Now;
            var simulatedBalance = currentBalance;
            var predictedCredits = _creditPredictor.PredictNext5Credits(creditHistory);
            
            DateTime? adjustedDepletionDate = null;
            var willDeplete = true;

            for (int day = 0; day < MaxSimulationDays; day++)
            {
                var checkDate = currentDate.AddDays(day);
                simulatedBalance -= dailyBurnRate;

                var creditOnThisDay = predictedCredits.FirstOrDefault(c => c.PredictedDate.Date == checkDate.Date);
                if (creditOnThisDay != null)
                {
                    simulatedBalance += (decimal)creditOnThisDay.PredictedAmount;
                }

                if (simulatedBalance <= 0)
                {
                    adjustedDepletionDate = checkDate;
                    willDeplete = true;
                    break;
                }
            }

            if (!adjustedDepletionDate.HasValue)
            {
                willDeplete = false;
            }

            return new SmartDepletionForecast
            {
                MathematicalDepletionDate = mathematicalDepletionDate,
                AdjustedDepletionDate = adjustedDepletionDate,
                WillDeplete = willDeplete,
                NextPredictedCredit = nextCreditPrediction,
                PredictedCredits = predictedCredits,
                Message = willDeplete ? "Balance will deplete even with predicted credits" : "Balance will be maintained with predicted credits"
            };
        }
    }

    public class SmartDepletionForecast
    {
        public DateTime MathematicalDepletionDate { get; set; }
        public DateTime? AdjustedDepletionDate { get; set; }
        public bool WillDeplete { get; set; }
        public CreditPrediction? NextPredictedCredit { get; set; }
        public List<CreditPrediction> PredictedCredits { get; set; } = new List<CreditPrediction>();
        public string Message { get; set; } = string.Empty;
    }
}
