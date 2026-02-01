using DistributorBalanceAnalyzer.Core.Models;

namespace DistributorBalanceAnalyzer.Core.Services;

/// <summary>
/// Service for analyzing distributor balance data and projecting depletion scenarios
/// </summary>
public class DistributorBalanceAnalyzer
{
    /// <summary>
    /// Analyzes distributor balance data and generates projection with multiple scenarios
    /// </summary>
    public DistributorBalanceProjection AnalyzeDistributor(DistributorBalanceData data, string distributorName)
    {
        var projection = new DistributorBalanceProjection
        {
            DistributorId = data.DistributorId,
            DistributorName = distributorName,
            CurrentBalance = data.CurrentBalance,
            TotalDebits = data.TotalDebits,
            TotalCredits = data.TotalCredits,
            NetChange = data.TotalCredits - data.TotalDebits,
            TransactionCount = data.TransactionCount,
            LastTransactionDate = data.LastTransactionDate,
            GeneratedAt = DateTime.Now
        };

        // Calculate scenarios for different time windows
        projection.Scenario7d = CalculateScenario("7-Day Trend", data.Debits7d, data.FirstDebit7d, data.LastDebit7d, 7, data.CurrentBalance);
        projection.Scenario30d = CalculateScenario("30-Day Trend", data.Debits30d, data.FirstDebit30d, data.LastDebit30d, 30, data.CurrentBalance);
        projection.Scenario90d = CalculateScenario("90-Day Trend", data.Debits90d, data.FirstDebit90d, data.LastDebit90d, 90, data.CurrentBalance);
        
        // Calculate all-time scenario
        var allTimeDays = (data.LastTransactionDate - data.FirstTransactionDate).Days;
        if (allTimeDays == 0) allTimeDays = 1;
        projection.ScenarioAllTime = CalculateScenario("All-Time Average", data.TotalDebits, data.FirstTransactionDate, data.LastTransactionDate, allTimeDays, data.CurrentBalance);

        // Store burn rates
        projection.DailyBurnRate7d = projection.Scenario7d.DailyBurnRate;
        projection.DailyBurnRate30d = projection.Scenario30d.DailyBurnRate;
        projection.DailyBurnRate90d = projection.Scenario90d.DailyBurnRate;
        projection.DailyBurnRateAllTime = projection.ScenarioAllTime.DailyBurnRate;

        // Calculate recommended scenario (weighted average)
        projection.RecommendedScenario = CalculateRecommendedScenario(projection);

        return projection;
    }

    /// <summary>
    /// Calculates a depletion scenario for a specific time window
    /// </summary>
    private DepletionScenario CalculateScenario(string scenarioName, decimal totalDebits, DateTime? firstDate, DateTime? lastDate, int expectedDays, decimal currentBalance)
    {
        var scenario = new DepletionScenario
        {
            ScenarioName = scenarioName
        };

        // Check if we have sufficient data
        if (firstDate == null || lastDate == null || totalDebits == 0)
        {
            scenario.HasSufficientData = false;
            scenario.Status = BalanceStatus.NoActivity;
            scenario.DataPointDays = 0;
            return scenario;
        }

        // Calculate actual days in the period
        var actualDays = (lastDate.Value - firstDate.Value).Days;
        if (actualDays == 0) actualDays = 1;
        scenario.DataPointDays = actualDays;

        // Check if we have at least 50% of expected data points
        scenario.HasSufficientData = actualDays >= (expectedDays * 0.5);

        // Calculate daily burn rate
        scenario.DailyBurnRate = totalDebits / actualDays;

        // Calculate days remaining and depletion date
        if (scenario.DailyBurnRate > 0)
        {
            scenario.DaysRemaining = (int)(currentBalance / scenario.DailyBurnRate);
            scenario.DepletionDate = DateTime.Now.AddDays(scenario.DaysRemaining);
        }
        else
        {
            scenario.DaysRemaining = int.MaxValue;
            scenario.DepletionDate = null;
        }

        // Determine status
        scenario.Status = DetermineStatus(currentBalance, scenario.DaysRemaining);

        return scenario;
    }

    /// <summary>
    /// Calculates recommended scenario using weighted average (50% 7d, 30% 30d, 20% 90d)
    /// </summary>
    private DepletionScenario CalculateRecommendedScenario(DistributorBalanceProjection projection)
    {
        var scenario = new DepletionScenario
        {
            ScenarioName = "Recommended (Weighted)"
        };

        decimal totalWeight = 0;
        decimal weightedBurnRate = 0;

        // Weight scenarios based on data sufficiency
        if (projection.Scenario7d.HasSufficientData)
        {
            weightedBurnRate += projection.Scenario7d.DailyBurnRate * 0.5m;
            totalWeight += 0.5m;
        }

        if (projection.Scenario30d.HasSufficientData)
        {
            weightedBurnRate += projection.Scenario30d.DailyBurnRate * 0.3m;
            totalWeight += 0.3m;
        }

        if (projection.Scenario90d.HasSufficientData)
        {
            weightedBurnRate += projection.Scenario90d.DailyBurnRate * 0.2m;
            totalWeight += 0.2m;
        }

        // If no recent scenarios have data, fall back to all-time
        if (totalWeight == 0 && projection.ScenarioAllTime.HasSufficientData)
        {
            weightedBurnRate = projection.ScenarioAllTime.DailyBurnRate;
            totalWeight = 1;
        }

        if (totalWeight > 0)
        {
            scenario.DailyBurnRate = weightedBurnRate / totalWeight;
            scenario.HasSufficientData = true;

            if (scenario.DailyBurnRate > 0)
            {
                scenario.DaysRemaining = (int)(projection.CurrentBalance / scenario.DailyBurnRate);
                scenario.DepletionDate = DateTime.Now.AddDays(scenario.DaysRemaining);
            }
            else
            {
                scenario.DaysRemaining = int.MaxValue;
                scenario.DepletionDate = null;
            }

            scenario.Status = DetermineStatus(projection.CurrentBalance, scenario.DaysRemaining);
        }
        else
        {
            scenario.HasSufficientData = false;
            scenario.Status = BalanceStatus.NoActivity;
        }

        return scenario;
    }

    /// <summary>
    /// Determines balance status based on current balance and days remaining
    /// </summary>
    private BalanceStatus DetermineStatus(decimal currentBalance, int daysRemaining)
    {
        if (currentBalance <= 0)
        {
            return BalanceStatus.Depleted;
        }

        if (daysRemaining == int.MaxValue)
        {
            return BalanceStatus.Increasing;
        }

        if (daysRemaining < 7)
        {
            return BalanceStatus.Critical;
        }

        if (daysRemaining < 30)
        {
            return BalanceStatus.Warning;
        }

        if (daysRemaining < 90)
        {
            return BalanceStatus.Moderate;
        }

        return BalanceStatus.Healthy;
    }
}
