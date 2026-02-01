using Microsoft.AspNetCore.Mvc;
using DistributorBalanceAnalyzer.Core.Models;
using DistributorBalanceAnalyzer.Core.Services;

namespace DistributorBalanceAnalyzer.Api.Controllers;

/// <summary>
/// Controller for distributor balance analysis operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DistributorBalanceController : ControllerBase
{
    private readonly DistributorBalanceService _service;
    private readonly ILogger<DistributorBalanceController> _logger;

    public DistributorBalanceController(
        DistributorBalanceService service,
        ILogger<DistributorBalanceController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the balance projection for a distributor
    /// </summary>
    /// <param name="distributorId">The distributor ID</param>
    /// <returns>Balance projection with multiple scenarios</returns>
    /// <response code="200">Returns the balance projection</response>
    /// <response code="404">Distributor not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{distributorId}")]
    [ProducesResponseType(typeof(DistributorBalanceProjection), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DistributorBalanceProjection>> GetDistributorBalance(string distributorId)
    {
        try
        {
            _logger.LogInformation("Getting balance projection for distributor: {DistributorId}", distributorId);

            var projection = await _service.GetDistributorBalanceProjectionAsync(distributorId);

            if (projection == null)
            {
                _logger.LogWarning("Distributor not found: {DistributorId}", distributorId);
                return NotFound(new { message = $"Distributor {distributorId} not found" });
            }

            _logger.LogInformation("Successfully retrieved balance projection for distributor: {DistributorId}", distributorId);
            return Ok(projection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance projection for distributor: {DistributorId}", distributorId);
            return StatusCode(500, new { message = "An error occurred while processing the request", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets alert information for a distributor based on depletion threshold
    /// </summary>
    /// <param name="distributorId">The distributor ID</param>
    /// <param name="daysThreshold">Alert threshold in days (default: 30)</param>
    /// <returns>Alert information</returns>
    /// <response code="200">Returns alert information</response>
    /// <response code="404">Distributor not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{distributorId}/alert")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetDistributorAlert(string distributorId, [FromQuery] int daysThreshold = 30)
    {
        try
        {
            _logger.LogInformation("Getting alert for distributor: {DistributorId} with threshold: {DaysThreshold}", 
                distributorId, daysThreshold);

            var projection = await _service.GetDistributorBalanceProjectionAsync(distributorId);

            if (projection == null)
            {
                _logger.LogWarning("Distributor not found: {DistributorId}", distributorId);
                return NotFound(new { message = $"Distributor {distributorId} not found" });
            }

            var scenario = projection.RecommendedScenario;
            var shouldAlert = scenario.HasSufficientData && 
                              scenario.DaysRemaining < daysThreshold &&
                              scenario.DaysRemaining != int.MaxValue;

            var alertResponse = new
            {
                distributorId = projection.DistributorId,
                distributorName = projection.DistributorName,
                currentBalance = projection.CurrentBalance,
                daysRemaining = scenario.DaysRemaining == int.MaxValue ? -1 : scenario.DaysRemaining,
                depletionDate = scenario.DepletionDate,
                status = scenario.Status.ToString(),
                shouldAlert = shouldAlert,
                alertThreshold = daysThreshold,
                message = shouldAlert 
                    ? $"⚠️ ALERT: Balance will be depleted in {scenario.DaysRemaining} days (threshold: {daysThreshold} days)"
                    : scenario.DaysRemaining == int.MaxValue
                        ? "✅ Balance is increasing - no alert"
                        : $"✅ Balance is healthy - {scenario.DaysRemaining} days remaining (threshold: {daysThreshold} days)",
                dailyBurnRate = scenario.DailyBurnRate
            };

            _logger.LogInformation("Alert check completed for distributor: {DistributorId}. Alert: {ShouldAlert}", 
                distributorId, shouldAlert);

            return Ok(alertResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert for distributor: {DistributorId}", distributorId);
            return StatusCode(500, new { message = "An error occurred while processing the request", error = ex.Message });
        }
    }
}
