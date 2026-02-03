using Microsoft.Extensions.Configuration;
using DistributorBalanceAnalyzer.Core.Analyzers;
using DistributorBalanceAnalyzer.Core.Models;
using DistributorBalanceAnalyzer.Core.Repositories;
using DistributorBalanceAnalyzer.Core.Services;

namespace DistributorBalanceAnalyzer.Console;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("OracleDb");
            var distributorId = configuration["DistributorId"];

            if (string.IsNullOrEmpty(connectionString))
            {
                System.Console.WriteLine("Error: Connection string not found in appsettings.json");
                return;
            }

            if (string.IsNullOrEmpty(distributorId))
            {
                System.Console.WriteLine("Error: DistributorId not found in appsettings.json");
                return;
            }

            // Create repository and service instances
            var repository = new BalanceRepository(connectionString);
            var analyzer = new BalanceAnalyzer();
            var service = new HierarchyBalanceService(repository, analyzer);

            System.Console.WriteLine("Fetching distributor balance data...");
            System.Console.WriteLine();

            // Call GetCompleteHierarchyReportAsync
            var report = await service.GetCompleteHierarchyReportAsync(distributorId);

            // Display formatted output
            DisplayReport(report);
        }
        catch (Exception ex)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"Error: {ex.Message}");
            System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            System.Console.ResetColor();
        }
    }

    static void DisplayReport(HierarchyBalanceReport report)
    {
        // Display Distributor balance
        DisplaySection("LEVEL 1: DISTRIBUTOR BALANCE");

        if (report.DistributorBalance != null)
        {
            var dist = report.DistributorBalance;
            System.Console.WriteLine($"Entity: {dist.EntityName} ({dist.EntityId})");
            System.Console.WriteLine($"Current Balance: {dist.CurrentBalance:N2}");
            System.Console.WriteLine();

            // Display all scenarios
            System.Console.WriteLine("Depletion Scenarios:");
            DisplayScenario("7-Day", dist.Scenario7d);
            DisplayScenario("30-Day", dist.Scenario30d);
            DisplayScenario("90-Day", dist.Scenario90d);
            System.Console.WriteLine();

            // Display recommended scenario with emphasis
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.Write("⭐ RECOMMENDED: ");
            System.Console.ResetColor();

            if (dist.RecommendedScenario.HasSufficientData)
            {
                System.Console.WriteLine($"Days Remaining: {dist.RecommendedScenario.DaysRemaining} | Depletion: {dist.RecommendedScenario.DepletionDate:yyyy-MM-dd}");

                // Show alert for distributor if < 30 days
                if (dist.RecommendedScenario.DaysRemaining < 30)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"🚨 CRITICAL: Distributor balance will be depleted in {dist.RecommendedScenario.DaysRemaining} days!");
                    System.Console.ResetColor();
                }
            }
            else
            {
                System.Console.WriteLine("Insufficient data for projection");
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"Transaction Statistics:");
            System.Console.WriteLine($"  Total Transactions: {dist.TransactionCount}");
            System.Console.WriteLine($"  Total Credits: {dist.TotalCredits:N2}");
            System.Console.WriteLine($"  Total Debits: {dist.TotalDebits:N2}");
            System.Console.WriteLine($"  Net Change: {dist.NetChange:N2}");
            System.Console.WriteLine($"  Last Transaction: {dist.LastTransactionDate:yyyy-MM-dd HH:mm:ss}");
            System.Console.WriteLine();
        }
        else
        {
            System.Console.WriteLine("No distributor data found.");
            System.Console.WriteLine();
        }

        // Display Shop summary
        DisplaySection("LEVEL 2: SHOPS SUMMARY");
        System.Console.WriteLine($"Total Shops: {report.TotalShops}");
        
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"🔴 Critical: {report.CriticalShops}");
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine($"🟡 Warning: {report.WarningShops}");
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine($"🟢 Healthy: {report.HealthyShops}");
        System.Console.ResetColor();
        System.Console.WriteLine();

        // Display top 10 critical shops
        var criticalShops = report.ShopBalances
            .Where(s => s.RecommendedScenario.HasSufficientData)
            .OrderBy(s => s.RecommendedScenario.DaysRemaining)
            .Take(10)
            .ToList();

        if (criticalShops.Any())
        {
            System.Console.WriteLine("Top 10 Critical Shops:");
            System.Console.WriteLine($"{"Shop Name",-40} {"Balance",15} {"Days Left",12} {"Status",10}");
            System.Console.WriteLine(new string('-', 80));

            foreach (var shop in criticalShops)
            {
                var statusColor = GetStatusColor(shop.Status);
                System.Console.Write($"{shop.EntityName,-40} {shop.CurrentBalance,15:N2} ");
                
                System.Console.ForegroundColor = statusColor;
                System.Console.Write($"{shop.RecommendedScenario.DaysRemaining,12} {shop.Status,10}");
                System.Console.ResetColor();
                System.Console.WriteLine();
            }
            System.Console.WriteLine();
        }

        // Display Agent summary
        DisplaySection("LEVEL 3: AGENTS SUMMARY");
        System.Console.WriteLine($"Total Agents: {report.TotalAgents}");
        
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"🔴 Critical: {report.CriticalAgents}");
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine($"🟡 Warning: {report.WarningAgents}");
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine($"🟢 Healthy: {report.HealthyAgents}");
        System.Console.ResetColor();
        System.Console.WriteLine();

        // Display top 20 critical agents
        var criticalAgents = report.AgentBalances
            .Where(a => a.RecommendedScenario.HasSufficientData)
            .OrderBy(a => a.RecommendedScenario.DaysRemaining)
            .Take(20)
            .ToList();

        if (criticalAgents.Any())
        {
            System.Console.WriteLine("Top 20 Critical Agents:");
            System.Console.WriteLine($"{"Agent Name",-40} {"Balance",15} {"Days Left",12} {"Status",10}");
            System.Console.WriteLine(new string('-', 80));

            foreach (var agent in criticalAgents)
            {
                var statusColor = GetStatusColor(agent.Status);
                System.Console.Write($"{agent.EntityName,-40} {agent.CurrentBalance,15:N2} ");
                
                System.Console.ForegroundColor = statusColor;
                System.Console.Write($"{agent.RecommendedScenario.DaysRemaining,12} {agent.Status,10}");
                System.Console.ResetColor();
                System.Console.WriteLine();
            }
            System.Console.WriteLine();
        }

        // Display overall summary
        DisplaySection("OVERALL SUMMARY");
        System.Console.WriteLine($"Total Balance Across All Levels: {report.TotalBalanceAllLevels:N2}");
        System.Console.WriteLine($"Total Daily Burn Rate (All Levels): {report.TotalDailyBurnAllLevels:N2}");
        System.Console.WriteLine($"Report Generated: {report.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine();
    }

    static void DisplaySection(string title)
    {
        System.Console.WriteLine(new string('═', 63));
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine($"  {title}");
        System.Console.ResetColor();
        System.Console.WriteLine(new string('═', 63));
    }

    static void DisplayScenario(string name, DepletionScenario scenario)
    {
        System.Console.Write($"  {name,-10}: ");
        
        if (scenario.HasSufficientData)
        {
            System.Console.WriteLine($"Burn Rate: {scenario.DailyBurnRate:N2}/day | Days Left: {scenario.DaysRemaining} | Depletion: {scenario.DepletionDate:yyyy-MM-dd}");
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine("Insufficient data");
            System.Console.ResetColor();
        }
    }

    static ConsoleColor GetStatusColor(BalanceStatus status)
    {
        return status switch
        {
            BalanceStatus.Critical => ConsoleColor.Red,
            BalanceStatus.Warning => ConsoleColor.Yellow,
            BalanceStatus.Healthy => ConsoleColor.Green,
            BalanceStatus.Increasing => ConsoleColor.Cyan,
            BalanceStatus.NoActivity => ConsoleColor.DarkGray,
            BalanceStatus.Depleted => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}
