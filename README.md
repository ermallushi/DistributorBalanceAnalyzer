# Distributor Balance Analyzer

A comprehensive .NET 8.0 solution for analyzing distributor balance transactions from Oracle database and predicting when the balance will be depleted. This tool provides multiple projection scenarios with weighted algorithms to help make informed business decisions.

## Features

- 📊 **Multiple Projection Scenarios**: Analyzes balance depletion using 7-day, 30-day, 90-day, and all-time trends
- 🎯 **Weighted Algorithm**: Smart recommendation combining recent trends (50% 7d, 30% 30d, 20% 90d)
- 💻 **Console Application**: Rich colored terminal output with emoji indicators for quick status assessment
- 🌐 **RESTful API**: Web API with Swagger documentation for easy integration
- 🔮 **Oracle Database Support**: Optimized SQL queries aggregate 60K+ records at database level for performance
- 🚨 **Status Alerts**: Automatic classification (Critical, Warning, Moderate, Healthy, Increasing, NoActivity, Depleted)
- 📈 **Detailed Metrics**: Comprehensive transaction analysis including burn rates, depletion dates, and trend analysis

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Oracle Database with access to `TBLTCREDITBALANCETRANSACTION` and `TBLMACCOUNT` tables
- Oracle connection credentials

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/ermallushi/DistributorBalanceAnalyzer.git
cd DistributorBalanceAnalyzer
```

### 2. Configure Database Connection

Update the connection string in `appsettings.json` files:

**Console App**: `DistributorBalanceAnalyzer.Console/appsettings.json`
**Web API**: `DistributorBalanceAnalyzer.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=your_username;Password=your_password;Data Source=your_oracle_server:1521/your_service_name"
  }
}
```

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run Console Application

```bash
cd DistributorBalanceAnalyzer.Console
dotnet run ACC000008528
```

Or use the default from appsettings.json:

```bash
dotnet run
```

### 5. Run Web API

```bash
cd DistributorBalanceAnalyzer.Api
dotnet run
```

Access Swagger UI at: `https://localhost:5001/swagger`

## Usage Examples

### Console Application

The console app displays rich colored output with comprehensive analysis:

```bash
dotnet run ACC000008528
```

**Output includes**:
- Distributor information and current balance
- Transaction metrics (total debits, credits, net change)
- Recommended scenario with weighted algorithm
- Detailed breakdown of all 4 scenarios
- Color-coded status indicators (🔴 Critical, ⚠️ Warning, ✅ Healthy, etc.)

### Web API

#### Get Balance Projection

```bash
GET /api/distributorbalance/{distributorId}
```

**Example Request**:
```bash
curl -X GET "https://localhost:5001/api/distributorbalance/ACC000008528"
```

**Example Response**:
```json
{
  "distributorId": "ACC000008528",
  "distributorName": "Example Distributor",
  "currentBalance": 15000.50,
  "dailyBurnRate7d": 250.75,
  "dailyBurnRate30d": 220.30,
  "dailyBurnRate90d": 200.15,
  "dailyBurnRateAllTime": 180.50,
  "scenario7d": {
    "scenarioName": "7-Day Trend",
    "dailyBurnRate": 250.75,
    "daysRemaining": 59,
    "depletionDate": "2026-04-01T12:00:00",
    "dataPointDays": 7,
    "hasSufficientData": true,
    "status": "Moderate"
  },
  "recommendedScenario": {
    "scenarioName": "Recommended (Weighted)",
    "dailyBurnRate": 235.25,
    "daysRemaining": 63,
    "depletionDate": "2026-04-05T12:00:00",
    "dataPointDays": 0,
    "hasSufficientData": true,
    "status": "Moderate"
  },
  "totalDebits": 50000.00,
  "totalCredits": 65000.50,
  "netChange": 15000.50,
  "transactionCount": 1234,
  "lastTransactionDate": "2026-02-01T10:30:00",
  "generatedAt": "2026-02-01T17:45:00"
}
```

#### Get Alert Status

```bash
GET /api/distributorbalance/{distributorId}/alert?daysThreshold=30
```

**Example Request**:
```bash
curl -X GET "https://localhost:5001/api/distributorbalance/ACC000008528/alert?daysThreshold=30"
```

**Example Response**:
```json
{
  "distributorId": "ACC000008528",
  "distributorName": "Example Distributor",
  "currentBalance": 15000.50,
  "daysRemaining": 63,
  "depletionDate": "2026-04-05T12:00:00",
  "status": "Moderate",
  "shouldAlert": false,
  "alertThreshold": 30,
  "message": "✅ Balance is healthy - 63 days remaining (threshold: 30 days)",
  "dailyBurnRate": 235.25
}
```

## Project Structure

```
DistributorBalanceAnalyzer/
├── DistributorBalanceAnalyzer.sln
├── .gitignore
├── README.md
├── DistributorBalanceAnalyzer.Core/          # Core business logic library
│   ├── DistributorBalanceAnalyzer.Core.csproj
│   ├── Models/
│   │   ├── BalanceStatus.cs                  # Status enumeration
│   │   ├── DistributorBalanceData.cs         # Database query result model
│   │   ├── DepletionScenario.cs              # Single scenario projection
│   │   └── DistributorBalanceProjection.cs   # Complete analysis result
│   ├── Repositories/
│   │   └── DistributorBalanceRepository.cs   # Oracle database access
│   └── Services/
│       ├── DistributorBalanceAnalyzer.cs     # Analysis engine
│       └── DistributorBalanceService.cs      # Service orchestrator
├── DistributorBalanceAnalyzer.Console/        # Console application
│   ├── DistributorBalanceAnalyzer.Console.csproj
│   ├── Program.cs                            # Rich colored CLI output
│   └── appsettings.json
└── DistributorBalanceAnalyzer.Api/            # Web API
    ├── DistributorBalanceAnalyzer.Api.csproj
    ├── Program.cs                            # API configuration
    ├── appsettings.json
    └── Controllers/
        └── DistributorBalanceController.cs   # API endpoints
```

## How It Works

### 1. Data Aggregation

The system uses a single optimized SQL query to aggregate transaction data at the database level:

- Processes 60,000+ transaction records efficiently
- Aggregates data into multiple time windows (7d, 30d, 90d, all-time)
- Separates debits (DR) and credits (CR)
- Calculates current balance from most recent transaction

### 2. Analysis Algorithm

For each time window, the analyzer:

1. **Calculates Daily Burn Rate**: `Total Debits ÷ Days in Period`
2. **Projects Days Remaining**: `Current Balance ÷ Daily Burn Rate`
3. **Determines Depletion Date**: `Today + Days Remaining`
4. **Validates Data Sufficiency**: Requires ≥50% expected data points
5. **Classifies Status**: Based on days remaining and balance

### 3. Status Classification

- **Depleted** (❌): Balance ≤ 0
- **Critical** (🔴): < 7 days remaining
- **Warning** (⚠️): 7-30 days remaining
- **Moderate** (🟡): 30-90 days remaining
- **Healthy** (✅): > 90 days remaining
- **Increasing** (📈): Credits exceed debits
- **NoActivity** (⏸️): Insufficient transaction data

### 4. Weighted Recommendation

The recommended scenario combines multiple projections:

```
Weighted Burn Rate = (7d × 50%) + (30d × 30%) + (90d × 20%)
```

This approach:
- Prioritizes recent trends (7-day window)
- Balances with medium-term patterns (30-day)
- Considers long-term stability (90-day)
- Falls back to all-time average if recent data is insufficient

## Configuration

### Connection String Format

```
User Id=username;Password=password;Data Source=host:port/service_name
```

### Console App Settings

`DistributorBalanceAnalyzer.Console/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleDb": "your_connection_string"
  },
  "DistributorId": "ACC000008528"  // Default distributor ID
}
```

### API Settings

`DistributorBalanceAnalyzer.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "OracleDb": "your_connection_string"
  }
}
```

## API Endpoints

### GET /api/distributorbalance/{distributorId}

Returns complete balance projection with all scenarios.

**Parameters**:
- `distributorId` (path): Distributor account ID

**Responses**:
- `200 OK`: Returns DistributorBalanceProjection
- `404 Not Found`: Distributor not found
- `500 Internal Server Error`: Server error

### GET /api/distributorbalance/{distributorId}/alert

Returns alert status based on threshold.

**Parameters**:
- `distributorId` (path): Distributor account ID
- `daysThreshold` (query, optional): Alert threshold in days (default: 30)

**Responses**:
- `200 OK`: Returns alert information
- `404 Not Found`: Distributor not found
- `500 Internal Server Error`: Server error

## Testing

### Build and Test

```bash
# Build all projects
dotnet build

# Run a specific project
dotnet run --project DistributorBalanceAnalyzer.Console

# Test the API
dotnet run --project DistributorBalanceAnalyzer.Api
```

### Manual Testing

1. **Console App**: Run with different distributor IDs to verify analysis
2. **API**: Use Swagger UI at `/swagger` for interactive testing
3. **Integration**: Test with various data scenarios (low balance, high activity, etc.)

## Dependencies

### Core Library
- **Oracle.ManagedDataAccess.Core** (23.4.0): Oracle database connectivity

### Console Application
- **Microsoft.Extensions.Configuration** (8.0.0): Configuration management
- **Microsoft.Extensions.Configuration.Json** (8.0.0): JSON configuration provider

### Web API
- **Swashbuckle.AspNetCore** (6.5.0): Swagger/OpenAPI documentation

## Architecture

### Clean Architecture Principles

- **Separation of Concerns**: Core logic separated from presentation layers
- **Dependency Inversion**: Projects depend on abstractions, not implementations
- **Single Responsibility**: Each class has one clear purpose

### Design Patterns

- **Repository Pattern**: Data access abstraction
- **Service Layer**: Business logic orchestration
- **Dependency Injection**: Loose coupling between components

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see below for details.

```
MIT License

Copyright (c) 2026 Ermal Lushi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Author

**Ermal Lushi**

- GitHub: [@ermallushi](https://github.com/ermallushi)

## Acknowledgments

- Built with .NET 8.0
- Oracle database integration
- Clean Architecture principles
- RESTful API design best practices