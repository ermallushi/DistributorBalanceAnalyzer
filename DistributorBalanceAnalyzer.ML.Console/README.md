# ML-Enhanced Distributor Balance Analyzer

A machine learning-powered console application that predicts future credit top-ups and provides intelligent balance depletion forecasts using ML.NET.

## Overview

This application complements the traditional mathematical balance analyzer with advanced machine learning capabilities:

- **Credit Pattern Prediction**: Forecasts when the next credit top-up will occur and its amount
- **Anomaly Detection**: Identifies unusual spending spikes and deviations from historical patterns
- **Smart Depletion Forecast**: Combines mathematical burn rate with ML predictions for accurate balance projections
- **Hierarchical Analysis**: Analyzes balance and predictions across all levels of the distributor hierarchy

## Features

### 1. Hierarchical Level Analysis
- **Multi-Level Support**: Analyzes distributor hierarchies from Level 1 (own balance) through multiple sub-distributor levels
- **Per-Level Predictions**: ML predictions and anomaly detection for each hierarchy level
- **Aggregate Views**: Shows total entities, balances, debits, and credits per level
- **Entity Details**: Displays individual entity analysis within each level

### 2. Credit Pattern Prediction
- Predicts the next 5 credit top-up events
- Uses SSA (Singular Spectrum Analysis) time-series forecasting
- Provides confidence scores for each prediction
- Learns seasonal patterns from historical data

### 3. Anomaly Detection
- Detects unusual spending spikes
- Identifies abnormal transaction patterns
- Alerts on deviations from historical behavior
- Highlights recent anomalies (last 30 days)

### 4. Smart Depletion Forecast
- Combines mathematical burn rate with ML credit predictions
- Adjusts depletion date based on predicted credits
- Calculates whether balance will actually deplete
- Provides actionable recommendations

## Configuration

Update `appsettings.json` with your database connection and distributor ID:

```json
{
  "ConnectionStrings": {
    "OracleDb": "Data Source=YOUR_TNS_NAME;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;"
  },
  "DistributorId": "ACC000008528"
}
```

## Running the Application

```bash
dotnet run --project DistributorBalanceAnalyzer.ML.Console
```

## Output

The application displays hierarchical analysis:

1. **Hierarchy Overview**: Shows total levels found and entity count
2. **Per-Level Analysis**: 
   - Level description (e.g., "Level 1 - Distributor Own Balance")
   - Entity count at this level
   - Aggregated totals (balance, debits, credits)
3. **Entity-Level ML Predictions** (for each entity in the level):
   - Current balance and burn rate
   - Next credit prediction with confidence
   - Smart depletion forecast
   - Recent anomalies count
4. **Performance Summary**: Total analysis time

### Hierarchy Levels

- **Level 1**: Distributor's own balance (direct account)
- **Level 2**: Direct sub-distributors (first-tier network)
- **Level 3**: Indirect sub-distributors (second-tier network)
- **Level 4+**: Additional network levels (if present)

The application automatically detects the hierarchy structure using Oracle's `CONNECT BY` hierarchical queries. If no hierarchy exists (single-level structure), it analyzes only the distributor's own balance.

## Requirements

- .NET 8.0
- Oracle Database with transaction history
- At least 10 credit transactions for ML training
- At least 30 days of spending data for anomaly detection

## ML Models

### Credit Prediction Service
- **Algorithm**: SSA (Singular Spectrum Analysis)
- **Input Features**: Amount, Date, DaysSinceLastCredit, DayOfWeek, DayOfMonth, Month, CurrentBalance
- **Output**: 5 future credit predictions with confidence scores
- **Training Data**: Last 365 days of credit transactions

### Anomaly Detection Service
- **Algorithm**: Spike Detection SSA
- **Input Features**: DailyDebits, TransactionCount, AverageTransactionSize, DayOfWeek
- **Output**: Anomaly flags and scores for each day
- **Training Data**: Last 90 days of debit transactions

### Smart Depletion Service
- **Algorithm**: Simulation-based forecasting
- **Simulation Window**: 365 days
- **Logic**: Combines daily burn rate with predicted credits to simulate balance over time
- **Output**: Adjusted depletion date considering future credits

## Dependencies

- **Microsoft.ML** (3.0.1): Core ML.NET library
- **Microsoft.ML.TimeSeries** (3.0.1): Time-series forecasting capabilities
- **Microsoft.Extensions.Configuration** (8.0.0): Configuration management
- **Microsoft.Extensions.Configuration.Json** (8.0.0): JSON configuration support

## Project Structure

```
DistributorBalanceAnalyzer.ML.Console/
├── Models/
│   ├── CreditPredictionModel.cs    # Credit prediction data models
│   └── SpendingAnomalyModel.cs     # Anomaly detection data models
├── Services/
│   ├── CreditPredictionService.cs  # ML credit forecasting
│   ├── AnomalyDetectionService.cs  # Spending anomaly detection
│   ├── SmartDepletionService.cs    # Smart depletion forecasting
│   └── MLDataPreparationService.cs # Data preparation for ML
├── Program.cs                       # Main application entry point
├── appsettings.json                # Configuration file
└── DistributorBalanceAnalyzer.ML.Console.csproj
```

## Example Output

```
╔═══════════════════════════════════════════════════════════╗
║  ML-ENHANCED DISTRIBUTOR BALANCE ANALYZER                 ║
╚═══════════════════════════════════════════════════════════╝

Analyzing distributor: ACC000008528...

📊 Analyzing hierarchy structure...
   ✓ Found 3 hierarchy level(s)
   ✓ Total entities: 15

═══════════════════════════════════════════════════════════
  LEVEL 1 (DISTRIBUTOR - OWN BALANCE)
═══════════════════════════════════════════════════════════
Entities in this level: 1
Total Balance: 125,000.00
Total Debits: 450,000.00
Total Credits: 575,000.00

  Entity: ACC000008528
     Balance: 125,000.00
     Burn Rate: 1,250.00/day
     🤖 Next Credit: 2026-03-15 - 50,000.00 (85%)
     ✅ Healthy (maintained by predicted credits)

═══════════════════════════════════════════════════════════
  LEVEL 2 (DIRECT SUB-DISTRIBUTORS)
═══════════════════════════════════════════════════════════
Entities in this level: 5
Total Balance: 345,000.00
Total Debits: 1,250,000.00
Total Credits: 1,595,000.00

  Entity: ACC000012345
     Balance: 85,000.00
     Burn Rate: 950.00/day
     🤖 Next Credit: 2026-03-20 - 45,000.00 (78%)
     ✅ Healthy (maintained by predicted credits)

  Entity: ACC000012346
     Balance: 62,000.00
     Burn Rate: 820.00/day
     🤖 Next Credit: 2026-03-18 - 38,000.00 (82%)
     ⚠️  Depletion: 2026-05-22
     ⚠️  2 spending anomalies detected (last 30 days)

  Entity: ACC000012347
     Balance: 103,000.00
     Burn Rate: 1,100.00/day
     🤖 Next Credit: 2026-03-25 - 52,000.00 (80%)
     ✅ Healthy (maintained by predicted credits)

   ... and 2 more entities in Level 2 (Direct Sub-Distributors)

═══════════════════════════════════════════════════════════
  LEVEL 3 (INDIRECT SUB-DISTRIBUTORS)
═══════════════════════════════════════════════════════════
Entities in this level: 9
Total Balance: 523,000.00
Total Debits: 2,850,000.00
Total Credits: 3,373,000.00

  Entity: ACC000023451
     Balance: 48,000.00
     Burn Rate: 650.00/day
     (Insufficient data for ML: 8 credits)

  Entity: ACC000023452
     Balance: 67,000.00
     Burn Rate: 780.00/day
     🤖 Next Credit: 2026-03-22 - 35,000.00 (75%)
     ✅ Healthy (maintained by predicted credits)

  Entity: ACC000023453
     Balance: 91,000.00
     Burn Rate: 920.00/day
     🤖 Next Credit: 2026-03-28 - 48,000.00 (73%)
     ✅ Healthy (maintained by predicted credits)
     ⚠️  1 spending anomalies detected (last 30 days)

   ... and 6 more entities in Level 3 (Indirect Sub-Distributors)

═══════════════════════════════════════════════════════════
✓ Complete hierarchy analysis finished in 3842ms
═══════════════════════════════════════════════════════════
```

## Notes

- **Hierarchy Detection**: The system uses Oracle's `CONNECT BY` to traverse parent-child relationships in `tblmaccount`
- **Graceful Fallback**: If the database doesn't support hierarchy (no `parentaccountid` column), the system analyzes only the distributor's own balance (single level)
- ML models require sufficient historical data for accurate predictions (minimum 10 credits)
- Confidence scores decrease for longer-term predictions
- Anomaly detection requires at least 30 days of transaction data
- The smart depletion forecast simulates up to 365 days into the future
- Per-level analysis shows first 3 entities per level to keep output manageable
