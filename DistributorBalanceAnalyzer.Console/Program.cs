using Microsoft.Extensions.Configuration;
using DistributorBalanceAnalyzer.Core.Models;
using DistributorBalanceAnalyzer.Core.Repositories;
using DistributorBalanceAnalyzer.Core.Services;

namespace DistributorBalanceAnalyzer.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("OracleDb");
        if (string.IsNullOrEmpty(connectionString))
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("❌ Error: Oracle connection string not found in appsettings.json");
            System.Console.ResetColor();
            return;
        }

        // Get distributor ID from command line args or config
        var distributorId = args.Length > 0 ? args[0] : configuration["DistributorId"];
        if (string.IsNullOrEmpty(distributorId))
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("❌ Error: Distributor ID not provided");
            System.Console.WriteLine("Usage: DistributorBalanceAnalyzer.Console <distributorId>");
            System.Console.ResetColor();
            return;
        }

        try
        {
            // Create repository and service
            var repository = new DistributorBalanceRepository(connectionString);
            var service = new DistributorBalanceService(repository);

            // Display header
            System.Console.Clear();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            System.Console.WriteLine("║        DISTRIBUTOR BALANCE ANALYZER                           ║");
            System.Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            System.Console.ResetColor();
            System.Console.WriteLine();

            System.Console.Write("Analyzing distributor: ");
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(distributorId);
            System.Console.ResetColor();
            System.Console.WriteLine("Please wait...");
            System.Console.WriteLine();

            // Get projection
            var projection = await service.GetDistributorBalanceProjectionAsync(distributorId);

            if (projection == null)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"❌ No data found for distributor: {distributorId}");
                System.Console.ResetColor();
                return;
            }

            // Display results
            DisplayProjection(projection);
        }
        catch (Exception ex)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            System.Console.ResetColor();
        }
    }

    static void DisplayProjection(DistributorBalanceProjection projection)
    {
        // Basic Information
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.WriteLine("  DISTRIBUTOR INFORMATION");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.ResetColor();
        System.Console.WriteLine($"ID:             {projection.DistributorId}");
        System.Console.WriteLine($"Name:           {projection.DistributorName}");
        System.Console.WriteLine($"Generated:      {projection.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine();

        // Current Balance
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.WriteLine("  CURRENT BALANCE");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.ResetColor();
        
        var balanceColor = projection.CurrentBalance > 0 ? ConsoleColor.Green : ConsoleColor.Red;
        System.Console.ForegroundColor = balanceColor;
        System.Console.WriteLine($"Balance:        {projection.CurrentBalance:N2}");
        System.Console.ResetColor();
        System.Console.WriteLine();

        // Transaction Metrics
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.WriteLine("  TRANSACTION METRICS");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.ResetColor();
        System.Console.WriteLine($"Total Transactions: {projection.TransactionCount}");
        System.Console.WriteLine($"Total Debits:       {projection.TotalDebits:N2}");
        System.Console.WriteLine($"Total Credits:      {projection.TotalCredits:N2}");
        
        var netChangeColor = projection.NetChange >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
        System.Console.ForegroundColor = netChangeColor;
        System.Console.WriteLine($"Net Change:         {projection.NetChange:N2}");
        System.Console.ResetColor();
        
        if (projection.LastTransactionDate.HasValue)
        {
            System.Console.WriteLine($"Last Transaction:   {projection.LastTransactionDate.Value:yyyy-MM-dd}");
        }
        System.Console.WriteLine();

        // Recommended Scenario
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.WriteLine("  RECOMMENDED SCENARIO (WEIGHTED)");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.ResetColor();
        DisplayScenario(projection.RecommendedScenario, isRecommended: true);
        System.Console.WriteLine();

        // All Scenarios
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.WriteLine("  DETAILED SCENARIOS");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.ResetColor();
        
        DisplayScenario(projection.Scenario7d);
        System.Console.WriteLine();
        
        DisplayScenario(projection.Scenario30d);
        System.Console.WriteLine();
        
        DisplayScenario(projection.Scenario90d);
        System.Console.WriteLine();
        
        DisplayScenario(projection.ScenarioAllTime);
        System.Console.WriteLine();
    }

    static void DisplayScenario(DepletionScenario scenario, bool isRecommended = false)
    {
        var statusIcon = GetStatusIcon(scenario.Status);
        var statusColor = GetStatusColor(scenario.Status);

        System.Console.Write($"{scenario.ScenarioName}: ");
        System.Console.ForegroundColor = statusColor;
        System.Console.WriteLine($"{statusIcon} {scenario.Status}");
        System.Console.ResetColor();

        if (!scenario.HasSufficientData)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine("  ⚠️  Insufficient data for this scenario");
            System.Console.ResetColor();
            return;
        }

        System.Console.WriteLine($"  Daily Burn Rate:   {scenario.DailyBurnRate:N2}");
        System.Console.WriteLine($"  Days Remaining:    {(scenario.DaysRemaining == int.MaxValue ? "∞ (Increasing)" : scenario.DaysRemaining.ToString())}");
        
        if (scenario.DepletionDate.HasValue)
        {
            System.Console.WriteLine($"  Depletion Date:    {scenario.DepletionDate.Value:yyyy-MM-dd}");
        }
        
        System.Console.WriteLine($"  Data Point Days:   {scenario.DataPointDays}");
    }

    static string GetStatusIcon(BalanceStatus status)
    {
        return status switch
        {
            BalanceStatus.Critical => "🔴",
            BalanceStatus.Warning => "⚠️",
            BalanceStatus.Moderate => "🟡",
            BalanceStatus.Healthy => "✅",
            BalanceStatus.Increasing => "📈",
            BalanceStatus.NoActivity => "⏸️",
            BalanceStatus.Depleted => "❌",
            _ => "❓"
        };
    }

    static ConsoleColor GetStatusColor(BalanceStatus status)
    {
        return status switch
        {
            BalanceStatus.Critical => ConsoleColor.Red,
            BalanceStatus.Warning => ConsoleColor.Yellow,
            BalanceStatus.Moderate => ConsoleColor.DarkYellow,
            BalanceStatus.Healthy => ConsoleColor.Green,
            BalanceStatus.Increasing => ConsoleColor.Cyan,
            BalanceStatus.NoActivity => ConsoleColor.Gray,
            BalanceStatus.Depleted => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}
