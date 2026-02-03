using System.Collections.Concurrent;
using DistributorBalanceAnalyzer.Core.Models;

namespace DistributorBalanceAnalyzer.Core.Analyzers;

public class BalanceAnalyzer
{
    public List<BalanceProjection> AnalyzeBalances(List<BalanceAggregated> balances)
    {
        var projections = new ConcurrentBag<BalanceProjection>();

        // Process in parallel for performance
        Parallel.ForEach(balances, balance =>
        {
            var projection = AnalyzeSingleBalance(balance);
            projections.Add(projection);
        });

        // Sort by Level, then DaysRemaining
        return projections
            .OrderBy(p => p.Level)
            .ThenBy(p => p.RecommendedScenario.DaysRemaining)
            .ToList();
    }

    private BalanceProjection AnalyzeSingleBalance(BalanceAggregated balance)
    {
        // Calculate 3 scenarios
        var scenario7d = CalculateScenario(
            "7-Day",
            balance.Debits7d,
            balance.FirstDebit7d,
            balance.LastDebit7d,
            balance.CurrentBalance,
            7
        );

        var scenario30d = CalculateScenario(
            "30-Day",
            balance.Debits30d,
            balance.FirstDebit30d,
            balance.LastDebit30d,
            balance.CurrentBalance,
            30
        );

        var scenario90d = CalculateScenario(
            "90-Day",
            balance.Debits90d,
            balance.FirstDebit90d,
            balance.LastDebit90d,
            balance.CurrentBalance,
            90
        );

        // Calculate recommended scenario
        var recommendedScenario = CalculateRecommendedScenario(scenario7d, scenario30d, scenario90d, balance.CurrentBalance);

        // Calculate net change
        var netChange = balance.TotalCredits - balance.TotalDebits;

        // Determine status
        var status = DetermineStatus(recommendedScenario, netChange);

        return new BalanceProjection
        {
            EntityId = balance.EntityId,
            EntityName = balance.EntityName,
            Level = balance.Level,
            ParentId = balance.ParentId,
            CurrentBalance = balance.CurrentBalance,
            DailyBurnRate7d = scenario7d.DailyBurnRate,
            DailyBurnRate30d = scenario30d.DailyBurnRate,
            DailyBurnRate90d = scenario90d.DailyBurnRate,
            Scenario7d = scenario7d,
            Scenario30d = scenario30d,
            Scenario90d = scenario90d,
            RecommendedScenario = recommendedScenario,
            TotalDebits = balance.TotalDebits,
            TotalCredits = balance.TotalCredits,
            NetChange = netChange,
            TransactionCount = balance.TransactionCount,
            LastTransactionDate = balance.LastTransactionDate,
            Status = status
        };
    }

    private DepletionScenario CalculateScenario(
        string name,
        decimal debits,
        DateTime? start,
        DateTime? end,
        decimal balance,
        int expectedDays)
    {
        var scenario = new DepletionScenario
        {
            ScenarioName = name,
            DailyBurnRate = 0,
            DaysRemaining = 0,
            DepletionDate = null,
            DataPointDays = 0,
            HasSufficientData = false
        };

        // Calculate actual days in the window
        if (start.HasValue && end.HasValue)
        {
            var actualDays = (end.Value - start.Value).Days;
            if (actualDays == 0) actualDays = 1; // Avoid division by zero

            scenario.DataPointDays = actualDays;

            // Check if sufficient data (at least 50% of expected days)
            scenario.HasSufficientData = actualDays >= expectedDays * 0.5;

            if (scenario.HasSufficientData && debits > 0)
            {
                // Calculate daily burn rate
                scenario.DailyBurnRate = debits / actualDays;

                // Calculate days remaining
                if (scenario.DailyBurnRate > 0)
                {
                    var daysRemaining = balance / scenario.DailyBurnRate;
                    scenario.DaysRemaining = (int)Math.Ceiling(daysRemaining);
                    scenario.DepletionDate = DateTime.Now.AddDays(scenario.DaysRemaining);
                }
                else
                {
                    scenario.DaysRemaining = int.MaxValue;
                }
            }
        }

        return scenario;
    }

    private DepletionScenario CalculateRecommendedScenario(
        DepletionScenario s7d,
        DepletionScenario s30d,
        DepletionScenario s90d,
        decimal balance)
    {
        var recommended = new DepletionScenario
        {
            ScenarioName = "Recommended",
            DailyBurnRate = 0,
            DaysRemaining = 0,
            DepletionDate = null,
            DataPointDays = 0,
            HasSufficientData = false
        };

        // Weighted average: 50% 7-day + 30% 30-day + 20% 90-day
        // Only include scenarios with sufficient data
        var validScenarios = new List<(DepletionScenario scenario, decimal weight)>();

        if (s7d.HasSufficientData)
            validScenarios.Add((s7d, 0.5m));
        if (s30d.HasSufficientData)
            validScenarios.Add((s30d, 0.3m));
        if (s90d.HasSufficientData)
            validScenarios.Add((s90d, 0.2m));

        if (validScenarios.Count == 0)
        {
            // No sufficient data
            return recommended;
        }

        // Normalize weights if not all scenarios are available
        var totalWeight = validScenarios.Sum(s => s.weight);
        var normalizedScenarios = validScenarios.Select(s => (s.scenario, normalizedWeight: s.weight / totalWeight)).ToList();

        // Calculate weighted average burn rate
        var weightedBurnRate = normalizedScenarios.Sum(s => s.scenario.DailyBurnRate * s.normalizedWeight);
        recommended.DailyBurnRate = weightedBurnRate;
        recommended.HasSufficientData = true;

        // Calculate days remaining
        if (weightedBurnRate > 0)
        {
            var daysRemaining = balance / weightedBurnRate;
            recommended.DaysRemaining = (int)Math.Ceiling(daysRemaining);
            recommended.DepletionDate = DateTime.Now.AddDays(recommended.DaysRemaining);
        }
        else
        {
            recommended.DaysRemaining = int.MaxValue;
        }

        // Sum data point days from all valid scenarios
        recommended.DataPointDays = validScenarios.Sum(s => s.scenario.DataPointDays);

        return recommended;
    }

    private BalanceStatus DetermineStatus(DepletionScenario scenario, decimal netChange)
    {
        // Check if depleted
        if (scenario.DaysRemaining == 0)
            return BalanceStatus.Depleted;

        // Check if no activity
        if (!scenario.HasSufficientData)
            return BalanceStatus.NoActivity;

        // Check if increasing
        if (netChange > 0)
            return BalanceStatus.Increasing;

        // Classify based on days remaining
        if (scenario.DaysRemaining < 7)
            return BalanceStatus.Critical;
        else if (scenario.DaysRemaining < 30)
            return BalanceStatus.Warning;
        else
            return BalanceStatus.Healthy;
    }
}
