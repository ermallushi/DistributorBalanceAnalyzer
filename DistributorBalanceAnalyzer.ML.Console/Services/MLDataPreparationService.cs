using DistributorBalanceAnalyzer.Core.Repositories;
using DistributorBalanceAnalyzer.ML.Models;

namespace DistributorBalanceAnalyzer.ML.Services
{
    public class MLDataPreparationService
    {
        private readonly DistributorBalanceRepository _repository;

        public MLDataPreparationService(DistributorBalanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CreditTransactionData>> PrepareCreditDataAsync(string distributorId)
        {
            // For this implementation, we'll use the distributorId as the entityId
            // In a real scenario, you might need to look up the entityId from distributor data
            var creditTransactions = await _repository.GetRawTransactionsAsync(distributorId, balanceEffect: "CR", daysBack: 365);

            var creditData = new List<CreditTransactionData>();
            DateTime? lastCreditDate = null;

            foreach (var tx in creditTransactions.OrderBy(t => t.TransactionDate))
            {
                var daysSinceLast = lastCreditDate.HasValue ? (tx.TransactionDate - lastCreditDate.Value).Days : 0;

                creditData.Add(new CreditTransactionData
                {
                    Date = tx.TransactionDate,
                    Amount = (float)tx.Amount,
                    DaysSinceLastCredit = daysSinceLast,
                    DayOfWeek = (int)tx.TransactionDate.DayOfWeek,
                    DayOfMonth = tx.TransactionDate.Day,
                    Month = tx.TransactionDate.Month,
                    CurrentBalance = (float)tx.NewBalance
                });

                lastCreditDate = tx.TransactionDate;
            }

            return creditData;
        }

        public async Task<List<DailySpendingData>> PrepareSpendingDataAsync(string distributorId)
        {
            // For this implementation, we'll use the distributorId as the entityId
            var debitTransactions = await _repository.GetRawTransactionsAsync(distributorId, balanceEffect: "DR", daysBack: 90);

            var dailyGroups = debitTransactions
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new DailySpendingData
                {
                    Date = g.Key,
                    DailyDebits = (float)g.Sum(t => t.Amount),
                    TransactionCount = g.Count(),
                    AverageTransactionSize = (float)g.Average(t => t.Amount),
                    DayOfWeek = (int)g.Key.DayOfWeek
                })
                .OrderBy(d => d.Date)
                .ToList();

            return dailyGroups;
        }
    }
}
