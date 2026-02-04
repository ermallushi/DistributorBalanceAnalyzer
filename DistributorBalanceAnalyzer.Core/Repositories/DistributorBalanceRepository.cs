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
