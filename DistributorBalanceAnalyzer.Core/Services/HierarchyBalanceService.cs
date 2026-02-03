using DistributorBalanceAnalyzer.Core.Analyzers;
using DistributorBalanceAnalyzer.Core.Models;
using DistributorBalanceAnalyzer.Core.Repositories;

namespace DistributorBalanceAnalyzer.Core.Services;

public class HierarchyBalanceService
{
    private readonly BalanceRepository _repository;
    private readonly BalanceAnalyzer _analyzer;

    public HierarchyBalanceService(BalanceRepository repository, BalanceAnalyzer analyzer)
    {
        _repository = repository;
        _analyzer = analyzer;
    }

    public async Task<HierarchyBalanceReport> GetCompleteHierarchyReportAsync(string distributorId)
    {
        // Get all balances from repository
        var balances = await _repository.GetCompleteHierarchyBalancesAsync(distributorId);

        // Analyze with BalanceAnalyzer
        var projections = _analyzer.AnalyzeBalances(balances);

        // Separate into distributor/shop/agent projections
        var distributorBalance = projections.FirstOrDefault(p => p.Level == 1);
        var shopBalances = projections.Where(p => p.Level == 2).ToList();
        var agentBalances = projections.Where(p => p.Level == 3).ToList();

        // Calculate summary statistics
        var report = new HierarchyBalanceReport
        {
            DistributorId = distributorId,
            DistributorName = distributorBalance?.EntityName ?? "Unknown",
            GeneratedDate = DateTime.Now,
            DistributorBalance = distributorBalance,
            
            ShopBalances = shopBalances,
            TotalShops = shopBalances.Count,
            CriticalShops = shopBalances.Count(s => s.Status == BalanceStatus.Critical),
            WarningShops = shopBalances.Count(s => s.Status == BalanceStatus.Warning),
            HealthyShops = shopBalances.Count(s => s.Status == BalanceStatus.Healthy),
            
            AgentBalances = agentBalances,
            TotalAgents = agentBalances.Count,
            CriticalAgents = agentBalances.Count(a => a.Status == BalanceStatus.Critical),
            WarningAgents = agentBalances.Count(a => a.Status == BalanceStatus.Warning),
            HealthyAgents = agentBalances.Count(a => a.Status == BalanceStatus.Healthy),
            
            TotalBalanceAllLevels = projections.Sum(p => p.CurrentBalance),
            TotalDailyBurnAllLevels = projections.Sum(p => p.RecommendedScenario.DailyBurnRate)
        };

        return report;
    }

    public async Task<BalanceProjection?> GetDistributorBalanceOnlyAsync(string distributorId)
    {
        // Get level 1 only
        var balances = await _repository.GetAggregatedBalanceByLevelAsync(distributorId, 1);
        
        if (balances.Count == 0)
            return null;

        // Analyze
        var projections = _analyzer.AnalyzeBalances(balances);
        
        return projections.FirstOrDefault();
    }

    public async Task<List<BalanceProjection>> GetCriticalEntitiesAsync(string distributorId, int daysThreshold = 30)
    {
        // Get all balances
        var balances = await _repository.GetCompleteHierarchyBalancesAsync(distributorId);
        
        // Analyze
        var projections = _analyzer.AnalyzeBalances(balances);
        
        // Return entities with DaysRemaining <= threshold, sorted by DaysRemaining
        return projections
            .Where(p => p.RecommendedScenario.HasSufficientData && p.RecommendedScenario.DaysRemaining <= daysThreshold)
            .OrderBy(p => p.RecommendedScenario.DaysRemaining)
            .ToList();
    }
}
