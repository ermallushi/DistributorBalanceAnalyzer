# ML-Enhanced Distributor Balance Analyzer

A machine learning-powered console application that predicts future credit top-ups and provides intelligent balance depletion forecasts using ML.NET.

## Overview

This application complements the traditional mathematical balance analyzer with advanced machine learning capabilities:

- **Credit Pattern Prediction**: Forecasts when the next credit top-up will occur and its amount
- **Anomaly Detection**: Identifies unusual spending spikes and deviations from historical patterns
- **Smart Depletion Forecast**: Combines mathematical burn rate with ML predictions for accurate balance projections

## Features

### 1. Credit Pattern Prediction
- Predicts the next 5 credit top-up events
- Uses SSA (Singular Spectrum Analysis) time-series forecasting
- Provides confidence scores for each prediction
- Learns seasonal patterns from historical data

### 2. Anomaly Detection
- Detects unusual spending spikes
- Identifies abnormal transaction patterns
- Alerts on deviations from historical behavior
- Highlights recent anomalies (last 30 days)

### 3. Smart Depletion Forecast
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

The application displays:

1. **Traditional Mathematical Analysis**: Current balance, burn rate, and depletion date
2. **ML-Enhanced Predictions**: 
   - Next credit prediction with date, amount, and confidence
   - Upcoming 5 credits forecast
   - Smart depletion forecast (mathematical vs ML-adjusted)
3. **Spending Anomalies**: Recent unusual spending patterns
4. **Recommendation**: Actionable advice based on ML analysis

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

🤖 Preparing ML training data...
   ✓ Found 45 historical credits
   ✓ Found 87 days of spending data

🤖 Training ML models...
✓ Analysis completed in 1234ms

═══════════════════════════════════════════════════════════
  TRADITIONAL MATHEMATICAL ANALYSIS
═══════════════════════════════════════════════════════════
Current Balance: 125,000.00
Daily Burn Rate: 1,250.00
Days Remaining: 100
Depletion Date: 2026-05-15

═══════════════════════════════════════════════════════════
  🤖 ML-ENHANCED PREDICTIONS
═══════════════════════════════════════════════════════════
📈 Next Credit Prediction:
   Date: 2026-03-15
   Amount: 50,000.00
   Confidence: 85%

📈 Upcoming Credits (Next 5):
   2026-03-15 - 50,000.00 (Confidence: 95%)
   2026-04-15 - 48,500.00 (Confidence: 81%)
   2026-05-15 - 51,200.00 (Confidence: 69%)
   2026-06-15 - 49,800.00 (Confidence: 58%)
   2026-07-15 - 50,100.00 (Confidence: 50%)

🎯 Smart Depletion Forecast:
   Mathematical: 2026-05-15
   ML-Adjusted: Will not deplete
   Will Deplete: ✅ NO
   Balance will be maintained with predicted credits

⚠️  Spending Anomalies Detected (Last 30 Days):
   2026-01-28 - Unusual spending pattern (Score: 2.45)
   2026-01-15 - Unusual spending pattern (Score: 1.89)

═══════════════════════════════════════════════════════════
  💡 RECOMMENDATION
═══════════════════════════════════════════════════════════
✅ Balance is healthy with predicted credit patterns
   Next top-up expected: 2026-03-15
```

## Notes

- ML models require sufficient historical data for accurate predictions (minimum 10 credits)
- Confidence scores decrease for longer-term predictions
- Anomaly detection requires at least 30 days of transaction data
- The smart depletion forecast simulates up to 365 days into the future
