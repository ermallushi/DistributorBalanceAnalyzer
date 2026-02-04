using DistributorBalanceAnalyzer.Core.Models;
using DistributorBalanceAnalyzer.Core.Repositories;

namespace DistributorBalanceAnalyzer.Core.Services;

/// <summary>
/// Service for retrieving and analyzing distributor balance projections
/// </summary>
public class DistributorBalanceService
{
    private readonly DistributorBalanceRepository _repository;
    private readonly DistributorBalanceAnalyzer _analyzer;

    public DistributorBalanceService(DistributorBalanceRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _analyzer = new DistributorBalanceAnalyzer();
    }

    /// <summary>
    /// Gets the complete balance projection for a distributor
    /// </summary>
    public async Task<DistributorBalanceProjection?> GetDistributorBalanceProjectionAsync(string distributorId)
    {
        // Fetch data from repository
        var data = await _repository.GetDistributorBalanceDataAsync(distributorId);
        
        if (data == null)
        {
            return null;
        }

        // Get distributor name
        var distributorName = await _repository.GetDistributorNameAsync(distributorId);

        // Analyze and return projection
        return _analyzer.AnalyzeDistributor(data, distributorName);
    }
}
