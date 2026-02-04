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

            // Get hierarchy levels
            System.Console.WriteLine("📊 Analyzing hierarchy structure...");
            var hierarchyLevels = await repository.GetHierarchyLevelsAsync(distributorId);
            
            System.Console.WriteLine($"   ✓ Found {hierarchyLevels.Count} hierarchy level(s)");
            System.Console.WriteLine($"   ✓ Total entities: {hierarchyLevels.Sum(l => l.EntityCount)}");
            System.Console.WriteLine();

            // Analyze each level
            foreach (var level in hierarchyLevels)
            {
                System.Console.WriteLine("═══════════════════════════════════════════════════════════");
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine($"  {level.LevelDescription.ToUpper()}");
                System.Console.ResetColor();
                System.Console.WriteLine("═══════════════════════════════════════════════════════════");
                System.Console.WriteLine($"Entities in this level: {level.EntityCount}");
                System.Console.WriteLine($"Total Balance: {level.TotalBalance:N2}");
                System.Console.WriteLine($"Total Debits: {level.TotalDebits:N2}");
                System.Console.WriteLine($"Total Credits: {level.TotalCredits:N2}");
                System.Console.WriteLine();

                // For each entity in the level, perform ML analysis
                foreach (var entityId in level.EntityIds.Take(3)) // Show first 3 entities per level
                {
                    await AnalyzeEntityAsync(
                        entityId,
                        repository,
                        balanceService,
                        dataPrep,
                        creditPredictor,
                        anomalyDetector,
                        smartDepletion,
                        level.Level);
                }

                if (level.EntityCount > 3)
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkGray;
                    System.Console.WriteLine($"   ... and {level.EntityCount - 3} more entities in {level.LevelDescription}");
                    System.Console.ResetColor();
                    System.Console.WriteLine();
                }
            }

            stopwatch.Stop();
            
            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"✓ Complete hierarchy analysis finished in {stopwatch.ElapsedMilliseconds}ms");
            System.Console.ResetColor();
            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            
            System.Console.WriteLine("\nPress any key to exit...");
            System.Console.ReadKey();
        }

        static async Task AnalyzeEntityAsync(
            string entityId,
            DistributorBalanceRepository repository,
            DistributorBalanceService balanceService,
            MLDataPreparationService dataPrep,
            CreditPredictionService creditPredictor,
            AnomalyDetectionService anomalyDetector,
            SmartDepletionService smartDepletion,
            int level)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"  Entity: {entityId}");
            System.Console.ResetColor();

            var distributorBalance = await balanceService.GetDistributorBalanceProjectionAsync(entityId);
            
            if (distributorBalance == null)
            {
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine("     (No balance data available)");
                System.Console.ResetColor();
                return;
            }

            System.Console.WriteLine($"     Balance: {distributorBalance.CurrentBalance:N2}");
            System.Console.WriteLine($"     Burn Rate: {distributorBalance.RecommendedScenario?.DailyBurnRate:N2}/day");
            
            // Get ML training data
            var creditHistory = await dataPrep.PrepareCreditDataAsync(entityId);
            var spendingHistory = await dataPrep.PrepareSpendingDataAsync(entityId);

            if (creditHistory.Count >= 10)
            {
                var nextCreditPrediction = creditPredictor.PredictNextCredit(creditHistory, distributorBalance.CurrentBalance);
                
                if (nextCreditPrediction != null)
                {
                    System.Console.WriteLine($"     🤖 Next Credit: {nextCreditPrediction.PredictedDate:yyyy-MM-dd} - {nextCreditPrediction.PredictedAmount:N2} ({nextCreditPrediction.Confidence:P0})");
                }

                var smartForecast = smartDepletion.GenerateForecast(
                    distributorBalance.CurrentBalance,
                    distributorBalance.RecommendedScenario?.DailyBurnRate ?? 0,
                    distributorBalance.RecommendedScenario?.DepletionDate ?? DateTime.Now,
                    creditHistory);

                if (smartForecast.WillDeplete && smartForecast.AdjustedDepletionDate.HasValue)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"     ⚠️  Depletion: {smartForecast.AdjustedDepletionDate:yyyy-MM-dd}");
                    System.Console.ResetColor();
                }
                else
                {
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine($"     ✅ Healthy (maintained by predicted credits)");
                    System.Console.ResetColor();
                }
            }
            else
            {
                System.Console.ForegroundColor = ConsoleColor.DarkGray;
                System.Console.WriteLine($"     (Insufficient data for ML: {creditHistory.Count} credits)");
                System.Console.ResetColor();
            }

            // Show recent anomalies
            if (spendingHistory.Count >= 30)
            {
                var anomalies = anomalyDetector.DetectSpendingAnomalies(spendingHistory);
                var recentAnomalies = anomalies.Where(a => a.IsAnomaly && a.Date >= DateTime.Now.AddDays(-30)).Count();
                
                if (recentAnomalies > 0)
                {
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"     ⚠️  {recentAnomalies} spending anomalies detected (last 30 days)");
                    System.Console.ResetColor();
                }
            }

            System.Console.WriteLine();
        }
    }
}
