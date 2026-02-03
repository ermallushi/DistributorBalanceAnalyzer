# Distributor Balance Analyzer - Usage Guide

## Quick Start

### 1. Configuration

Edit `DistributorBalanceAnalyzer.Console/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleDb": "Data Source=PROD_DB;User Id=myuser;Password=mypassword;"
  },
  "DistributorId": "ACC000008528"
}
```

**Configuration Parameters:**
- **Data Source**: Oracle TNS name or connection string
- **User Id**: Oracle database username
- **Password**: Oracle database password
- **DistributorId**: The account ID of the distributor to analyze

### 2. Build and Run

```bash
# Build the solution
cd /path/to/DistributorBalanceAnalyzer
dotnet build

# Run the console application
cd DistributorBalanceAnalyzer.Console
dotnet run
```

### 3. Expected Output

The application will display a comprehensive report with:

1. **Distributor Balance (Level 1)**
   - Current balance
   - Depletion scenarios (7-day, 30-day, 90-day)
   - Recommended scenario (weighted average)
   - Transaction statistics
   - Critical alerts (if < 30 days remaining)

2. **Shops Summary (Level 2)**
   - Total shops count
   - Critical/Warning/Healthy breakdown
   - Top 10 critical shops

3. **Agents Summary (Level 3)**
   - Total agents count
   - Critical/Warning/Healthy breakdown
   - Top 20 critical agents

4. **Overall Summary**
   - Total balance across all levels
   - Total daily burn rate
   - Report generation timestamp

## Understanding the Output

### Balance Status Classification

| Status | Days Remaining | Color | Description |
|--------|---------------|-------|-------------|
| **Critical** | < 7 days | 🔴 Red | Immediate action required |
| **Warning** | 7-30 days | 🟡 Yellow | Monitor closely |
| **Healthy** | > 30 days | 🟢 Green | Normal operation |
| **Increasing** | N/A | 🔵 Cyan | Balance is growing |
| **NoActivity** | N/A | ⚫ Gray | Insufficient data |
| **Depleted** | 0 days | ⚫ Dark Red | Balance exhausted |

### Depletion Scenarios

The analyzer calculates three scenarios based on recent activity:

1. **7-Day Scenario**
   - Uses last 7 days of debit transactions
   - Weight: 50% in recommended scenario
   - Best for short-term trends

2. **30-Day Scenario**
   - Uses last 30 days of debit transactions
   - Weight: 30% in recommended scenario
   - Good for medium-term trends

3. **90-Day Scenario**
   - Uses last 90 days of debit transactions
   - Weight: 20% in recommended scenario
   - Best for long-term trends

4. **Recommended Scenario**
   - Weighted average of all scenarios
   - Only includes scenarios with sufficient data
   - Most accurate prediction

### Example Output Interpretation

```
⭐ RECOMMENDED: Days Remaining: 14 | Depletion: 2026-02-17
```

This means:
- The distributor balance will be depleted in 14 days
- Expected depletion date is February 17, 2026
- Based on weighted average of recent burn rates

## Programmatic Usage

You can also use the library programmatically in your own applications:

### Example 1: Get Complete Hierarchy Report

```csharp
using DistributorBalanceAnalyzer.Core.Analyzers;
using DistributorBalanceAnalyzer.Core.Repositories;
using DistributorBalanceAnalyzer.Core.Services;

var connectionString = "Data Source=...";
var distributorId = "ACC000008528";

var repository = new BalanceRepository(connectionString);
var analyzer = new BalanceAnalyzer();
var service = new HierarchyBalanceService(repository, analyzer);

var report = await service.GetCompleteHierarchyReportAsync(distributorId);

Console.WriteLine($"Distributor: {report.DistributorName}");
Console.WriteLine($"Balance: {report.DistributorBalance.CurrentBalance:N2}");
Console.WriteLine($"Days Remaining: {report.DistributorBalance.RecommendedScenario.DaysRemaining}");
```

### Example 2: Get Critical Entities Only

```csharp
// Get all entities with less than 15 days remaining
var criticalEntities = await service.GetCriticalEntitiesAsync(distributorId, daysThreshold: 15);

foreach (var entity in criticalEntities)
{
    Console.WriteLine($"{entity.EntityName}: {entity.RecommendedScenario.DaysRemaining} days");
}
```

### Example 3: Get Distributor Balance Only

```csharp
var distributorBalance = await service.GetDistributorBalanceOnlyAsync(distributorId);

if (distributorBalance != null)
{
    Console.WriteLine($"Current Balance: {distributorBalance.CurrentBalance:N2}");
    Console.WriteLine($"7-Day Burn Rate: {distributorBalance.DailyBurnRate7d:N2}");
    Console.WriteLine($"Status: {distributorBalance.Status}");
}
```

## Performance Optimization

### Database Performance

The solution uses database-level aggregation for optimal performance:

- **Query Optimization**: All aggregations happen at the database level
- **Hierarchical Queries**: Uses Oracle's CONNECT BY for efficient tree traversal
- **Parallel Fetching**: Fetches all 3 levels concurrently
- **Index Usage**: Ensure indexes on `entityid`, `transactiondate`, and `accountid`

### Application Performance

- **Parallel Processing**: Uses `Parallel.ForEach` for balance analysis
- **Concurrent Collections**: Thread-safe collection for parallel operations
- **Memory Efficient**: Streams data without loading entire dataset into memory

### Recommended Database Indexes

```sql
-- Indexes for optimal performance
CREATE INDEX idx_transaction_entity_date 
  ON TBLTCREDITBALANCETRANSACTION(entityid, transactiondate);

CREATE INDEX idx_transaction_date 
  ON TBLTCREDITBALANCETRANSACTION(transactiondate);

CREATE INDEX idx_account_parent 
  ON tblmaccount(parentaccountid, accountid);

CREATE INDEX idx_account_status 
  ON tblmaccount(AccountStatusId);
```

## Troubleshooting

### Common Issues

**1. Connection Error**
```
Error: Oracle.ManagedDataAccess.Client.OracleException: ORA-12154: TNS:could not resolve the connect identifier
```
**Solution**: Verify TNS name in `appsettings.json` or use full connection string

**2. No Data Returned**
```
No distributor data found.
```
**Solution**: 
- Verify the DistributorId exists in the database
- Check that the account status is 'Active'
- Verify there are transactions in TBLTCREDITBALANCETRANSACTION

**3. Insufficient Data Warning**
```
⭐ RECOMMENDED: Insufficient data for projection
```
**Solution**: This means there's less than 50% of expected data points. The account may be new or inactive. Check transaction history.

### Logging

To enable detailed logging, modify the code to add console output in repository methods:

```csharp
// In BalanceRepository.cs
Console.WriteLine($"Fetching {balances.Count} balances for level {level}");
```

## Advanced Configuration

### Custom Time Windows

To analyze different time windows, modify the SQL queries in `BalanceRepository.cs`:

```sql
-- Change from 7 days to 14 days
SUM(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 14 THEN t.amount ELSE 0 END) / 1000000 as debits_14d
```

### Custom Weighted Recommendation

To adjust the weighted recommendation formula, modify `BalanceAnalyzer.cs`:

```csharp
// Change from 50/30/20 to 60/30/10
if (s7d.HasSufficientData)
    validScenarios.Add((s7d, 0.6m));  // Changed from 0.5m
if (s30d.HasSufficientData)
    validScenarios.Add((s30d, 0.3m));
if (s90d.HasSufficientData)
    validScenarios.Add((s90d, 0.1m)); // Changed from 0.2m
```

## Security Best Practices

1. **Never commit credentials**: Keep `appsettings.json` out of version control
2. **Use environment variables**: Store sensitive data in environment variables
3. **Least privilege**: Use database accounts with read-only access
4. **Connection pooling**: Oracle.ManagedDataAccess handles this automatically
5. **Secure storage**: Use Azure Key Vault, AWS Secrets Manager, or similar for production

## Support and Maintenance

For questions or issues:
1. Check this documentation first
2. Review the code comments in the source files
3. Examine the database schema requirements
4. Verify Oracle database connectivity and permissions

