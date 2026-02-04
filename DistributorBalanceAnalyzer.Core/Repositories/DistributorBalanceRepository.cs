using Oracle.ManagedDataAccess.Client;
using DistributorBalanceAnalyzer.Core.Models;

namespace DistributorBalanceAnalyzer.Core.Repositories;

/// <summary>
/// Repository for accessing distributor balance transaction data from Oracle database
/// </summary>
public class DistributorBalanceRepository
{
    private readonly string _connectionString;

    public DistributorBalanceRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Gets aggregated balance data for a distributor across multiple time windows
    /// </summary>
    public async Task<DistributorBalanceData?> GetDistributorBalanceDataAsync(string distributorId)
    {
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                t.entityid,
                COUNT(*) as transaction_count,
                MIN(t.transactiondate) as first_transaction_date,
                MAX(t.transactiondate) as last_transaction_date,
                MAX(t.newbalance) KEEP (DENSE_RANK LAST ORDER BY t.transactiondate) / 1000000 as current_balance,
                SUM(CASE WHEN t.balanceeffect = 'DR' THEN t.amount ELSE 0 END) / 1000000 as total_debits,
                SUM(CASE WHEN t.balanceeffect = 'CR' THEN t.amount ELSE 0 END) / 1000000 as total_credits,
                
                SUM(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 7 
                         THEN t.amount ELSE 0 END) / 1000000 as debits_7d,
                MIN(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 7 
                         THEN t.transactiondate END) as first_debit_7d,
                MAX(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 7 
                         THEN t.transactiondate END) as last_debit_7d,
                
                SUM(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 30 
                         THEN t.amount ELSE 0 END) / 1000000 as debits_30d,
                MIN(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 30 
                         THEN t.transactiondate END) as first_debit_30d,
                MAX(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 30 
                         THEN t.transactiondate END) as last_debit_30d,
                
                SUM(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 90 
                         THEN t.amount ELSE 0 END) / 1000000 as debits_90d,
                MIN(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 90 
                         THEN t.transactiondate END) as first_debit_90d,
                MAX(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 90 
                         THEN t.transactiondate END) as last_debit_90d
            FROM TBLTCREDITBALANCETRANSACTION t
            WHERE t.transactiondate >= ADD_MONTHS(TRUNC(SYSDATE, 'YYYY'), -12)
              AND t.transactiondate <= SYSDATE
              AND t.entityid = :distributorId
            GROUP BY t.entityid";

        using var command = new OracleCommand(sql, connection);
        command.Parameters.Add("distributorId", OracleDbType.Varchar2).Value = distributorId;

        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return new DistributorBalanceData
            {
                DistributorId = reader.GetString(0),
                TransactionCount = reader.GetInt32(1),
                FirstTransactionDate = reader.GetDateTime(2),
                LastTransactionDate = reader.GetDateTime(3),
                CurrentBalance = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                TotalDebits = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                TotalCredits = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                
                Debits7d = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                FirstDebit7d = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                LastDebit7d = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                
                Debits30d = reader.IsDBNull(10) ? 0 : reader.GetDecimal(10),
                FirstDebit30d = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                LastDebit30d = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                
                Debits90d = reader.IsDBNull(13) ? 0 : reader.GetDecimal(13),
                FirstDebit90d = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                LastDebit90d = reader.IsDBNull(15) ? null : reader.GetDateTime(15)
            };
        }

        return null;
    }

    /// <summary>
    /// Gets the distributor name from the account table
    /// </summary>
    public async Task<string> GetDistributorNameAsync(string distributorId)
    {
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT name FROM tblmaccount WHERE accountid = :distributorId";

        using var command = new OracleCommand(sql, connection);
        command.Parameters.Add("distributorId", OracleDbType.Varchar2).Value = distributorId;

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? distributorId;
    }

    /// <summary>
    /// Gets all entities in a distributor's hierarchy grouped by level
    /// </summary>
    public async Task<List<HierarchyLevelBalance>> GetHierarchyLevelsAsync(string distributorId, int maxLevels = 5)
    {
        var levels = new List<HierarchyLevelBalance>();
        
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        // Use Oracle hierarchical query to get all descendants organized by level
        // This assumes tblmaccount has parentaccountid column for hierarchy
        // If the schema is different, this query will need adjustment
        var hierarchySql = @"
            SELECT LEVEL as hierarchy_level, accountid
            FROM tblmaccount
            START WITH accountid = :distributorId
            CONNECT BY PRIOR accountid = parentaccountid
            AND LEVEL <= :maxLevels
            ORDER BY LEVEL, accountid";

        try
        {
            using var hierarchyCommand = new OracleCommand(hierarchySql, connection);
            hierarchyCommand.Parameters.Add("distributorId", OracleDbType.Varchar2).Value = distributorId;
            hierarchyCommand.Parameters.Add("maxLevels", OracleDbType.Int32).Value = maxLevels;

            var entitiesByLevel = new Dictionary<int, List<string>>();
            
            using var hierarchyReader = await hierarchyCommand.ExecuteReaderAsync();
            while (await hierarchyReader.ReadAsync())
            {
                var level = hierarchyReader.GetInt32(0);
                var entityId = hierarchyReader.GetString(1);
                
                if (!entitiesByLevel.ContainsKey(level))
                {
                    entitiesByLevel[level] = new List<string>();
                }
                entitiesByLevel[level].Add(entityId);
            }

            // For each level, get aggregated balance data
            foreach (var levelGroup in entitiesByLevel.OrderBy(kvp => kvp.Key))
            {
                var level = levelGroup.Key;
                var entityIds = levelGroup.Value;

                var balanceData = await GetAggregatedBalanceForEntitiesAsync(connection, entityIds);
                
                levels.Add(new HierarchyLevelBalance
                {
                    Level = level,
                    LevelDescription = GetLevelDescription(level),
                    EntityCount = entityIds.Count,
                    EntityIds = entityIds,
                    TotalBalance = balanceData.TotalBalance,
                    TotalDebits = balanceData.TotalDebits,
                    TotalCredits = balanceData.TotalCredits
                });
            }
        }
        catch (Oracle.ManagedDataAccess.Client.OracleException)
        {
            // If hierarchy query fails (e.g., parentaccountid column doesn't exist),
            // return single level with just the distributor
            levels.Add(new HierarchyLevelBalance
            {
                Level = 1,
                LevelDescription = "Distributor (Own Balance)",
                EntityCount = 1,
                EntityIds = new List<string> { distributorId },
                TotalBalance = 0,
                TotalDebits = 0,
                TotalCredits = 0
            });
            
            // Get actual balance for this single entity
            var singleBalance = await GetDistributorBalanceDataAsync(distributorId);
            if (singleBalance != null)
            {
                levels[0].TotalBalance = singleBalance.CurrentBalance;
                levels[0].TotalDebits = singleBalance.TotalDebits;
                levels[0].TotalCredits = singleBalance.TotalCredits;
            }
        }

        return levels;
    }

    private async Task<(decimal TotalBalance, decimal TotalDebits, decimal TotalCredits)> GetAggregatedBalanceForEntitiesAsync(
        OracleConnection connection, List<string> entityIds)
    {
        if (!entityIds.Any())
            return (0, 0, 0);

        var entityIdList = string.Join(",", entityIds.Select(id => $"'{id}'"));
        
        var sql = $@"
            SELECT 
                SUM(MAX(t.newbalance) KEEP (DENSE_RANK LAST ORDER BY t.transactiondate)) / 1000000 as total_balance,
                SUM(CASE WHEN t.balanceeffect = 'DR' THEN t.amount ELSE 0 END) / 1000000 as total_debits,
                SUM(CASE WHEN t.balanceeffect = 'CR' THEN t.amount ELSE 0 END) / 1000000 as total_credits
            FROM TBLTCREDITBALANCETRANSACTION t
            WHERE t.entityid IN ({entityIdList})
              AND t.transactiondate >= ADD_MONTHS(TRUNC(SYSDATE, 'YYYY'), -12)
              AND t.transactiondate <= SYSDATE
            GROUP BY t.entityid";

        using var command = new OracleCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        decimal totalBalance = 0, totalDebits = 0, totalCredits = 0;
        
        while (await reader.ReadAsync())
        {
            totalBalance += reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
            totalDebits += reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            totalCredits += reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
        }

        return (totalBalance, totalDebits, totalCredits);
    }

    private string GetLevelDescription(int level)
    {
        return level switch
        {
            1 => "Level 1 (Distributor - Own Balance)",
            2 => "Level 2 (Direct Sub-Distributors)",
            3 => "Level 3 (Indirect Sub-Distributors)",
            4 => "Level 4 (Third-Level Network)",
            5 => "Level 5 (Fourth-Level Network)",
            _ => $"Level {level}"
        };
    }

    /// <summary>
    /// Gets raw transaction history for ML training
    /// </summary>
    public async Task<List<BalanceTransaction>> GetRawTransactionsAsync(string entityId, string? balanceEffect = null, int daysBack = 365)
    {
        var transactions = new List<BalanceTransaction>();

        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT t.transactionid, t.entityid, t.transactiondate, t.amount / 1000000 as amount,
                   t.oldbalance / 1000000 as oldbalance, t.newbalance / 1000000 as newbalance,
                   t.event, t.balanceeffect
            FROM TBLTCREDITBALANCETRANSACTION t
            WHERE t.entityid = :entityId AND t.transactiondate >= SYSDATE - :daysBack";

        if (!string.IsNullOrEmpty(balanceEffect))
        {
            sql += " AND t.balanceeffect = :balanceEffect";
        }

        sql += " ORDER BY t.transactiondate ASC";

        using var command = new OracleCommand(sql, connection);
        command.Parameters.Add("entityId", OracleDbType.Varchar2).Value = entityId;
        command.Parameters.Add("daysBack", OracleDbType.Int32).Value = daysBack;
        
        if (!string.IsNullOrEmpty(balanceEffect))
        {
            command.Parameters.Add("balanceEffect", OracleDbType.Varchar2).Value = balanceEffect;
        }

        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            transactions.Add(new BalanceTransaction
            {
                TransactionId = reader.GetInt64(0),
                EntityId = reader.GetString(1),
                TransactionDate = reader.GetDateTime(2),
                Amount = reader.GetDecimal(3),
                OldBalance = reader.GetDecimal(4),
                NewBalance = reader.GetDecimal(5),
                Event = reader.GetString(6),
                BalanceEffect = reader.GetString(7)
            });
        }

        return transactions;
    }
}
