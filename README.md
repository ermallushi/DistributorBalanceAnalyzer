# Distributor Balance Analyzer

A complete .NET solution that analyzes credit balance transactions across a 3-level account hierarchy (Distributor → Shops → Agents) and predicts when balances will be depleted.

## Features

- ✅ **3-level hierarchy support** - Analyzes distributor, shops, and agents
- ✅ **Database aggregation** - Handles 60K+ records efficiently using Oracle database-level aggregation
- ✅ **Multiple time windows** - 7-day, 30-day, 90-day burn rates
- ✅ **Weighted recommendations** - Smart prediction based on recent trends (50% 7-day + 30% 30-day + 20% 90-day)
- ✅ **Parallel processing** - Fast analysis using all CPU cores
- ✅ **Status classification** - Critical/Warning/Healthy alerts
- ✅ **Hierarchical queries** - Oracle CONNECT BY for efficient tree traversal

## Project Structure

```
DistributorBalanceAnalyzer/
├── DistributorBalanceAnalyzer.sln
├── DistributorBalanceAnalyzer.Core/
│   ├── Models/
│   │   ├── AccountHierarchy.cs
│   │   ├── BalanceAggregated.cs
│   │   ├── BalanceProjection.cs
│   │   ├── BalanceTransaction.cs
│   │   └── HierarchyBalanceReport.cs
│   ├── Repositories/
│   │   └── BalanceRepository.cs
│   ├── Analyzers/
│   │   └── BalanceAnalyzer.cs
│   └── Services/
│       └── HierarchyBalanceService.cs
└── DistributorBalanceAnalyzer.Console/
    ├── Program.cs
    └── appsettings.json
```

## Database Schema

The solution connects to Oracle database with these tables:

- **TBLTCREDITBALANCETRANSACTION** - Transaction records
  - `transactionid` (NUMBER)
  - `entityid` (VARCHAR2) - Links to accountid
  - `transactiondate` (TIMESTAMP)
  - `amount` (NUMBER) - Stored in millionths (divide by 1,000,000)
  - `oldbalance` (NUMBER) - Stored in millionths
  - `newbalance` (NUMBER) - Stored in millionths
  - `event` (VARCHAR2) - e.g., "Order", "UPDATE CREDIT BALANCE"
  - `balanceeffect` (VARCHAR2) - "DR" (debit) or "CR" (credit)

- **tblmaccount** - Account hierarchy
  - `accountid` (VARCHAR2)
  - `accountnumber` (VARCHAR2)
  - `name` (VARCHAR2)
  - `AccountStatusId` (NUMBER)
  - `parentaccountid` (VARCHAR2)

- **TblSAccountStatus** - Account status lookup
  - `accountstatusid` (NUMBER)
  - `name` (VARCHAR2) - e.g., "Active"

### Account Hierarchy

- **Level 1**: Distributor (root)
- **Level 2**: Shops (children of distributor)
- **Level 3**: Agents (children of shops)

## Configuration

Update `appsettings.json` in the Console project with your Oracle connection details:

```json
{
  "ConnectionStrings": {
    "OracleDb": "Data Source=YOUR_TNS_NAME;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;"
  },
  "DistributorId": "ACC000008528"
}
```

## Building and Running

### Prerequisites

- .NET 8.0 SDK
- Oracle Database with the required schema
- Oracle.ManagedDataAccess.Core NuGet package (automatically restored)

### Build

```bash
dotnet build
```

### Run

```bash
cd DistributorBalanceAnalyzer.Console
dotnet run
```

## Output Example

```
═══════════════════════════════════════════════════════════
  LEVEL 1: DISTRIBUTOR BALANCE
═══════════════════════════════════════════════════════════
Entity: Main Distributor (ACC000008528)
Current Balance: 798,861.95

Depletion Scenarios:
  7-Day     : Burn Rate: 56,918.71/day | Days Left: 14 | Depletion: 2026-02-17
  30-Day    : Burn Rate: 45,123.45/day | Days Left: 18 | Depletion: 2026-02-21
  90-Day    : Burn Rate: 38,456.78/day | Days Left: 21 | Depletion: 2026-02-24

⭐ RECOMMENDED: Days Remaining: 14 | Depletion: 2026-02-15
🚨 CRITICAL: Distributor balance will be depleted in 14 days!

Transaction Statistics:
  Total Transactions: 12,456
  Total Credits: 5,234,567.89
  Total Debits: 4,435,705.94
  Net Change: 798,861.95
  Last Transaction: 2026-02-03 10:30:45

═══════════════════════════════════════════════════════════
  LEVEL 2: SHOPS SUMMARY
═══════════════════════════════════════════════════════════
Total Shops: 45
🔴 Critical: 5
🟡 Warning: 12
🟢 Healthy: 28

Top 10 Critical Shops:
Shop Name                                      Balance    Days Left     Status
--------------------------------------------------------------------------------
Shop Alpha                               12,345.67            3   Critical
Shop Beta                                45,678.90            5   Critical
...

═══════════════════════════════════════════════════════════
  LEVEL 3: AGENTS SUMMARY
═══════════════════════════════════════════════════════════
Total Agents: 1250
🔴 Critical: 23
🟡 Warning: 156
🟢 Healthy: 1071

Top 20 Critical Agents:
Agent Name                                     Balance    Days Left     Status
--------------------------------------------------------------------------------
Agent John Doe                            1,234.56            2   Critical
Agent Jane Smith                          2,345.67            4   Critical
...

═══════════════════════════════════════════════════════════
  OVERALL SUMMARY
═══════════════════════════════════════════════════════════
Total Balance Across All Levels: 15,234,567.89
Total Daily Burn Rate (All Levels): 123,456.78
Report Generated: 2026-02-03 13:37:15
```

## Core Components

### Models

- **AccountHierarchy** - Represents account hierarchy with level information
- **BalanceAggregated** - Aggregated transaction data with multi-window statistics
- **BalanceProjection** - Balance projection with depletion scenarios
- **DepletionScenario** - Individual scenario (7-day, 30-day, 90-day, recommended)
- **HierarchyBalanceReport** - Complete report with all hierarchy levels

### Repository

- **BalanceRepository** - Database access layer
  - `GetAccountHierarchyAsync()` - Retrieves account hierarchy using Oracle CONNECT BY
  - `GetAggregatedBalanceByLevelAsync()` - Aggregates transactions at database level
  - `GetCompleteHierarchyBalancesAsync()` - Gets balances for all levels in parallel

### Analyzer

- **BalanceAnalyzer** - Balance analysis engine
  - Uses parallel processing for performance
  - Calculates multiple depletion scenarios
  - Provides weighted recommendations
  - Classifies balance status

### Service

- **HierarchyBalanceService** - Business logic layer
  - `GetCompleteHierarchyReportAsync()` - Generates complete hierarchy report
  - `GetDistributorBalanceOnlyAsync()` - Gets distributor balance only
  - `GetCriticalEntitiesAsync()` - Returns critical entities below threshold

## Performance

- **Query execution**: < 5 seconds for 60K+ records (database-level aggregation)
- **Analysis**: < 1 second using parallel processing
- **Memory**: < 100MB total

## Balance Status Classification

- **Critical** - Less than 7 days remaining
- **Warning** - 7 to 30 days remaining
- **Healthy** - More than 30 days remaining
- **Increasing** - Balance is growing (credits > debits)
- **NoActivity** - Insufficient data for analysis
- **Depleted** - Balance already at zero

## Technical Details

### Depletion Calculation

1. **Daily Burn Rate** = Total Debits / Days in Window
2. **Days Remaining** = Current Balance / Daily Burn Rate
3. **Depletion Date** = Today + Days Remaining

### Weighted Recommendation

The recommended scenario uses a weighted average:
- 50% weight on 7-day trend (most recent)
- 30% weight on 30-day trend
- 20% weight on 90-day trend

Only scenarios with sufficient data (≥50% of expected days) are included.

## License

This project is provided as-is for educational and commercial use.