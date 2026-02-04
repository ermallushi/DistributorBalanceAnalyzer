using DistributorBalanceAnalyzer.Core.Repositories;
using DistributorBalanceAnalyzer.Core.Services;
using DistributorBalanceAnalyzer.ML.Services;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace DistributorBalanceAnalyzer.ML.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("OracleDb");
            var distributorId = configuration["DistributorId"] ?? "ACC000008528";

            var repository = new DistributorBalanceRepository(connectionString!);
            var balanceService = new DistributorBalanceService(repository);
            var dataPrep = new MLDataPreparationService(repository);
            
            var creditPredictor = new CreditPredictionService();
            var anomalyDetector = new AnomalyDetectionService();
            var smartDepletion = new SmartDepletionService(creditPredictor);

            System.Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            System.Console.WriteLine("║  ML-ENHANCED DISTRIBUTOR BALANCE ANALYZER                 ║");
            System.Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            System.Console.WriteLine();

            var stopwatch = Stopwatch.StartNew();
            System.Console.WriteLine($"Analyzing distributor: {distributorId}...\n");

            var distributorBalance = await balanceService.GetDistributorBalanceProjectionAsync(distributorId);
            
            if (distributorBalance == null)
            {
                System.Console.WriteLine("❌ No balance data found for distributor.");
                return;
            }

            System.Console.WriteLine("🤖 Preparing ML training data...");
            var creditHistory = await dataPrep.PrepareCreditDataAsync(distributorId);
            var spendingHistory = await dataPrep.PrepareSpendingDataAsync(distributorId);

            System.Console.WriteLine($"   ✓ Found {creditHistory.Count} historical credits");
            System.Console.WriteLine($"   ✓ Found {spendingHistory.Count} days of spending data");
            System.Console.WriteLine();

            System.Console.WriteLine("🤖 Training ML models...");
            
            var nextCreditPrediction = creditPredictor.PredictNextCredit(creditHistory, distributorBalance.CurrentBalance);
            var anomalies = anomalyDetector.DetectSpendingAnomalies(spendingHistory);
            var smartForecast = smartDepletion.GenerateForecast(
                distributorBalance.CurrentBalance,
                distributorBalance.RecommendedScenario?.DailyBurnRate ?? 0,
                distributorBalance.RecommendedScenario?.DepletionDate ?? DateTime.Now,
                creditHistory);

            stopwatch.Stop();
            System.Console.WriteLine($"✓ Analysis completed in {stopwatch.ElapsedMilliseconds}ms\n");

            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            System.Console.WriteLine("  TRADITIONAL MATHEMATICAL ANALYSIS");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            System.Console.WriteLine($"Current Balance: {distributorBalance.CurrentBalance:N2}");
            System.Console.WriteLine($"Daily Burn Rate: {distributorBalance.RecommendedScenario?.DailyBurnRate:N2}");
            System.Console.WriteLine($"Days Remaining: {distributorBalance.RecommendedScenario?.DaysRemaining}");
            System.Console.WriteLine($"Depletion Date: {distributorBalance.RecommendedScenario?.DepletionDate:yyyy-MM-dd}");
            System.Console.WriteLine();

            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            System.Console.WriteLine("  🤖 ML-ENHANCED PREDICTIONS");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════");

            if (nextCreditPrediction != null)
            {
                System.Console.WriteLine("📈 Next Credit Prediction:");
                System.Console.WriteLine($"   Date: {nextCreditPrediction.PredictedDate:yyyy-MM-dd}");
                System.Console.WriteLine($"   Amount: {nextCreditPrediction.PredictedAmount:N2}");
                System.Console.WriteLine($"   Confidence: {nextCreditPrediction.Confidence:P0}");
                System.Console.WriteLine();

                System.Console.WriteLine("📈 Upcoming Credits (Next 5):");
                foreach (var credit in smartForecast.PredictedCredits)
                {
                    System.Console.WriteLine($"   {credit.PredictedDate:yyyy-MM-dd} - {credit.PredictedAmount:N2} (Confidence: {credit.Confidence:P0})");
                }
                System.Console.WriteLine();
            }

            System.Console.WriteLine("🎯 Smart Depletion Forecast:");
            System.Console.WriteLine($"   Mathematical: {smartForecast.MathematicalDepletionDate:yyyy-MM-dd}");
            System.Console.WriteLine($"   ML-Adjusted: {(smartForecast.AdjustedDepletionDate?.ToString("yyyy-MM-dd") ?? "Will not deplete")}");
            System.Console.WriteLine($"   Will Deplete: {(smartForecast.WillDeplete ? "❌ YES" : "✅ NO")}");
            System.Console.WriteLine($"   {smartForecast.Message}");
            System.Console.WriteLine();

            var recentAnomalies = anomalies.Where(a => a.IsAnomaly && a.Date >= DateTime.Now.AddDays(-30)).OrderByDescending(a => a.Date).Take(5).ToList();

            if (recentAnomalies.Any())
            {
                System.Console.WriteLine("⚠️  Spending Anomalies Detected (Last 30 Days):");
                foreach (var anomaly in recentAnomalies)
                {
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"   {anomaly.Date:yyyy-MM-dd} - Unusual spending pattern (Score: {anomaly.Score:F2})");
                    System.Console.ResetColor();
                }
                System.Console.WriteLine();
            }

            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            System.Console.WriteLine("  💡 RECOMMENDATION");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════");

            if (smartForecast.WillDeplete && smartForecast.AdjustedDepletionDate.HasValue)
            {
                var daysUntil = (smartForecast.AdjustedDepletionDate.Value - DateTime.Now).Days;
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"🚨 URGENT: Top up required before {smartForecast.AdjustedDepletionDate:yyyy-MM-dd} ({daysUntil} days)");
                System.Console.ResetColor();
            }
            else
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("✅ Balance is healthy with predicted credit patterns");
                System.Console.WriteLine($"   Next top-up expected: {nextCreditPrediction?.PredictedDate:yyyy-MM-dd}");
                System.Console.ResetColor();
            }

            System.Console.WriteLine("\nPress any key to exit...");
            System.Console.ReadKey();
        }
    }
}
