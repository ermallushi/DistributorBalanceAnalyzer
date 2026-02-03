using System.Collections.Concurrent;
using Oracle.ManagedDataAccess.Client;
using DistributorBalanceAnalyzer.Core.Models;

namespace DistributorBalanceAnalyzer.Core.Repositories;

public class BalanceRepository
{
    private readonly string _connectionString;

    public BalanceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<AccountHierarchy>> GetAccountHierarchyAsync(string distributorId)
    {
        var accounts = new List<AccountHierarchy>();

        const string sql = @"
            SELECT a.accountid, a.accountnumber, a.name, d.name AS status, LEVEL AS lvl, PRIOR a.accountid AS parentaccountid
            FROM tblmaccount a
            JOIN TblSAccountStatus d ON a.AccountStatusId = d.accountstatusid
            START WITH a.accountid = :distributorId
            CONNECT BY PRIOR a.accountid = a.parentaccountid
            WHERE d.name = 'Active'";

        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new OracleCommand(sql, connection);
        command.Parameters.Add(new OracleParameter("distributorId", distributorId));

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            accounts.Add(new AccountHierarchy
            {
                AccountId = reader["accountid"]?.ToString() ?? string.Empty,
                AccountNumber = reader["accountnumber"]?.ToString() ?? string.Empty,
                AccountName = reader["name"]?.ToString() ?? string.Empty,
                Status = reader["status"]?.ToString() ?? string.Empty,
                Level = reader["lvl"] != DBNull.Value ? Convert.ToInt32(reader["lvl"]) : 0,
                ParentAccountId = reader["parentaccountid"] != DBNull.Value ? reader["parentaccountid"]?.ToString() : null
            });
        }

        return accounts;
    }

    public async Task<List<BalanceAggregated>> GetAggregatedBalanceByLevelAsync(string distributorId, int level)
    {
        var balances = new List<BalanceAggregated>();

        const string sql = @"
            SELECT 
                t.entityid,
                COUNT(*) as transaction_count,
                MIN(t.transactiondate) as first_transaction_date,
                MAX(t.transactiondate) as last_transaction_date,
                MAX(t.newbalance) KEEP (DENSE_RANK LAST ORDER BY t.transactiondate) / 1000000 as current_balance,
                SUM(CASE WHEN t.balanceeffect = 'DR' THEN t.amount ELSE 0 END) / 1000000 as total_debits,
                SUM(CASE WHEN t.balanceeffect = 'CR' THEN t.amount ELSE 0 END) / 1000000 as total_credits,
                SUM(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 7 THEN t.amount ELSE 0 END) / 1000000 as debits_7d,
                MIN(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 7 THEN t.transactiondate END) as first_debit_7d,
                MAX(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 7 THEN t.transactiondate END) as last_debit_7d,
                SUM(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 30 THEN t.amount ELSE 0 END) / 1000000 as debits_30d,
                MIN(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 30 THEN t.transactiondate END) as first_debit_30d,
                MAX(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 30 THEN t.transactiondate END) as last_debit_30d,
                SUM(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 90 THEN t.amount ELSE 0 END) / 1000000 as debits_90d,
                MIN(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 90 THEN t.transactiondate END) as first_debit_90d,
                MAX(CASE WHEN t.balanceeffect = 'DR' AND t.transactiondate >= SYSDATE - 90 THEN t.transactiondate END) as last_debit_90d
            FROM TBLTCREDITBALANCETRANSACTION t
            WHERE t.transactiondate >= ADD_MONTHS(TRUNC(SYSDATE, 'YYYY'), -12)
              AND t.transactiondate <= SYSDATE
              AND t.entityid IN (
                  SELECT accountid FROM (
                      SELECT a.accountid, LEVEL AS lvl
                      FROM tblmaccount a
                      JOIN TblSAccountStatus d ON a.AccountStatusId = d.accountstatusid
                      START WITH a.accountid = :distributorId
                      CONNECT BY PRIOR a.accountid = a.parentaccountid
                      WHERE d.name = 'Active'
                  )
                  WHERE lvl = :level
              )
            GROUP BY t.entityid";

        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new OracleCommand(sql, connection);
        command.Parameters.Add(new OracleParameter("distributorId", distributorId));
        command.Parameters.Add(new OracleParameter("level", level));

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            balances.Add(new BalanceAggregated
            {
                EntityId = reader["entityid"]?.ToString() ?? string.Empty,
                Level = level,
                TransactionCount = reader["transaction_count"] != DBNull.Value ? Convert.ToInt32(reader["transaction_count"]) : 0,
                FirstTransactionDate = reader["first_transaction_date"] != DBNull.Value ? Convert.ToDateTime(reader["first_transaction_date"]) : null,
                LastTransactionDate = reader["last_transaction_date"] != DBNull.Value ? Convert.ToDateTime(reader["last_transaction_date"]) : null,
                CurrentBalance = reader["current_balance"] != DBNull.Value ? Convert.ToDecimal(reader["current_balance"]) : 0,
                TotalDebits = reader["total_debits"] != DBNull.Value ? Convert.ToDecimal(reader["total_debits"]) : 0,
                TotalCredits = reader["total_credits"] != DBNull.Value ? Convert.ToDecimal(reader["total_credits"]) : 0,
                Debits7d = reader["debits_7d"] != DBNull.Value ? Convert.ToDecimal(reader["debits_7d"]) : 0,
                FirstDebit7d = reader["first_debit_7d"] != DBNull.Value ? Convert.ToDateTime(reader["first_debit_7d"]) : null,
                LastDebit7d = reader["last_debit_7d"] != DBNull.Value ? Convert.ToDateTime(reader["last_debit_7d"]) : null,
                Debits30d = reader["debits_30d"] != DBNull.Value ? Convert.ToDecimal(reader["debits_30d"]) : 0,
                FirstDebit30d = reader["first_debit_30d"] != DBNull.Value ? Convert.ToDateTime(reader["first_debit_30d"]) : null,
                LastDebit30d = reader["last_debit_30d"] != DBNull.Value ? Convert.ToDateTime(reader["last_debit_30d"]) : null,
                Debits90d = reader["debits_90d"] != DBNull.Value ? Convert.ToDecimal(reader["debits_90d"]) : 0,
                FirstDebit90d = reader["first_debit_90d"] != DBNull.Value ? Convert.ToDateTime(reader["first_debit_90d"]) : null,
                LastDebit90d = reader["last_debit_90d"] != DBNull.Value ? Convert.ToDateTime(reader["last_debit_90d"]) : null
            });
        }

        return balances;
    }

    public async Task<List<BalanceAggregated>> GetCompleteHierarchyBalancesAsync(string distributorId)
    {
        // Get hierarchy to map entity names
        var hierarchy = await GetAccountHierarchyAsync(distributorId);
        var hierarchyLookup = hierarchy.ToDictionary(h => h.AccountId, h => h);

        // Get balances for all three levels
        var allBalances = new ConcurrentBag<BalanceAggregated>();

        var tasks = new[]
        {
            Task.Run(async () =>
            {
                var level1 = await GetAggregatedBalanceByLevelAsync(distributorId, 1);
                foreach (var b in level1) allBalances.Add(b);
            }),
            Task.Run(async () =>
            {
                var level2 = await GetAggregatedBalanceByLevelAsync(distributorId, 2);
                foreach (var b in level2) allBalances.Add(b);
            }),
            Task.Run(async () =>
            {
                var level3 = await GetAggregatedBalanceByLevelAsync(distributorId, 3);
                foreach (var b in level3) allBalances.Add(b);
            })
        };

        await Task.WhenAll(tasks);

        // Enrich with hierarchy information
        var result = allBalances.ToList();
        foreach (var balance in result)
        {
            if (hierarchyLookup.TryGetValue(balance.EntityId, out var account))
            {
                balance.EntityName = account.AccountName;
                balance.ParentId = account.ParentAccountId;
            }
        }

        return result;
    }
}
