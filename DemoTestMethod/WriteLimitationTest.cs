using System.Collections.Concurrent;
using Snowflake.Data.Client;

namespace DemoTestMethod
{
    /// <summary>
    /// Demonstrates Snowflake write limitations:
    /// - DML on a table is serialized by a table lock; at most 20 statements may wait for the lock
    ///   (error 000625), and all others are aborted.
    /// - PRIMARY KEY is not enforced; uniqueness only comes from AUTOINCREMENT.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Snowflake Write Limitation")]
    public class WriteLimitationTest : TestSetup
    {
        private static SnowflakeConnection _dbConnectionInfo = null!;
        private const string BenchMarkTableName = "BenchmarkTest_StandardTable";

        // https://docs.snowflake.com/en/sql-reference/transactions#resource-locking
        private const int SnowflakeLockWaiterLimit = 20;
        private const int SnowflakeLockErrorCode = 625;
        private const int MaxRetries = 10;

        private static readonly ConcurrentDictionary<string, int> LockRetries = new();

        public TestContext TestContext { get; set; } = null!;

        [ClassInitialize]
        public static async Task ClassInitializeAsync(TestContext _)
        {
            _dbConnectionInfo = await GetSnowflakeConnection().ConfigureAwait(false);

            var createTableSql = $"""
                                  CREATE TABLE IF NOT EXISTS "{_dbConnectionInfo.DbName}"."{_dbConnectionInfo.Schema}"."{BenchMarkTableName}" (
                                      "Id" BIGINT AUTOINCREMENT START 1 INCREMENT 1,
                                      "BatchId" VARCHAR(100),
                                      "TaskId" NUMBER(10,0),
                                      "Value" VARCHAR(255),
                                      "CreatedAt" TIMESTAMP_NTZ DEFAULT CURRENT_TIMESTAMP(),
                                      PRIMARY KEY ("Id")
                                  );
                                  """;

            using var queryProvider = _dbConnectionInfo.GetQueryProvider();
            await queryProvider.ExecuteAsync(createTableSql).ConfigureAwait(false);
        }

        [ClassCleanup]
        public static async Task ClassCleanupAsync()
        {
            var dropTableSql = $@"DROP TABLE IF EXISTS ""{_dbConnectionInfo.DbName}"".""{_dbConnectionInfo.Schema}"".""{BenchMarkTableName}"";";

            using var queryProvider = _dbConnectionInfo.GetQueryProvider();
            await queryProvider.ExecuteAsync(dropTableSql).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(24, 200, 15)]
        public async Task Should_Insert_In_Parallel_And_Maintain_Unique_Primary_Keys(int parallelTasks, int batchSize, int iterations)
        {
            var batchId = Guid.NewGuid().ToString("N");
            var tasks = new List<Task>();

            for (var taskId = 0; taskId < parallelTasks; taskId++)
            {
                var currentTaskId = taskId;
                tasks.Add(Task.Run(async () => await InsertDataInBatchesAsync(batchId, currentTaskId, batchSize, iterations).ConfigureAwait(false)));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            TestContext.WriteLine($"Lock retries (error 000625) for batch {batchId}: {LockRetries.GetValueOrDefault(batchId)}");

            await VerifyUniquePrimaryKeysAsync(batchId, parallelTasks * batchSize * iterations).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(40, 500)]
        public async Task Should_Abort_Statements_When_More_Than_20_Wait_For_Table_Lock(int parallelTasks, int batchSize)
        {
            var batchId = Guid.NewGuid().ToString("N");
            var tasks = new List<Task>();

            for (var taskId = 0; taskId < parallelTasks; taskId++)
            {
                var currentTaskId = taskId;
                tasks.Add(Task.Run(async () => await InsertDataInBatchesAsync(batchId, currentTaskId, batchSize, iterations: 1, retryOnLock: false).ConfigureAwait(false)));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (SnowflakeDbException)
            {
                // expected: at least one statement is aborted because of the lock limit
            }

            var lockErrors = tasks
                .Where(t => t.IsFaulted)
                .SelectMany(t => t.Exception!.InnerExceptions)
                .OfType<SnowflakeDbException>()
                .Where(e => e.ErrorCode == SnowflakeLockErrorCode)
                .ToList();

            TestContext.WriteLine($"{parallelTasks} parallel INSERTs into one table: {lockErrors.Count} statements aborted because of the lock limit ({SnowflakeLockWaiterLimit} waiters).");
            if (lockErrors.Count > 0)
            {
                TestContext.WriteLine(lockErrors[0].Message);
            }

            Assert.IsTrue(parallelTasks > SnowflakeLockWaiterLimit + 1, "Test parameter must exceed the lock limit");
            Assert.IsGreaterThan(0, lockErrors.Count, "Snowflake should return error 000625 when more than 20 statements are waiting");
        }

        [TestMethod]
        [DataRow(10, 100, 1)]
        public async Task Should_Handle_High_Volume_Parallel_Inserts(int parallelTasks, int batchSize, int iterations)
        {
            var batchId = Guid.NewGuid().ToString("N");
            var tasks = new List<Task>();

            for (var taskId = 0; taskId < parallelTasks; taskId++)
            {
                var currentTaskId = taskId;
                tasks.Add(Task.Run(async () => await InsertDataInBatchesAsync(batchId, currentTaskId, batchSize, iterations).ConfigureAwait(false)));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await VerifyUniquePrimaryKeysAsync(batchId, parallelTasks * batchSize * iterations).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(3, 5, 10)]
        public async Task Should_Handle_Multiple_Small_Batches(int parallelTasks, int batchSize, int iterations)
        {
            var batchId = Guid.NewGuid().ToString("N");
            var tasks = new List<Task>();

            for (var taskId = 0; taskId < parallelTasks; taskId++)
            {
                var currentTaskId = taskId;
                tasks.Add(Task.Run(async () => await InsertDataInBatchesAsync(batchId, currentTaskId, batchSize, iterations).ConfigureAwait(false)));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await VerifyUniquePrimaryKeysAsync(batchId, parallelTasks * batchSize * iterations).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(5, 10, 5)]
        public async Task Should_Verify_AutoIncrement_Sequence(int parallelTasks, int batchSize, int iterations)
        {
            var batchId = Guid.NewGuid().ToString("N");
            var tasks = new List<Task<List<long>>>();

            for (var taskId = 0; taskId < parallelTasks; taskId++)
            {
                var currentTaskId = taskId;
                tasks.Add(Task.Run(async () => await InsertAndReturnIdsAsync(batchId, currentTaskId, batchSize, iterations).ConfigureAwait(false)));
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var allIds = results.SelectMany(r => r).ToList();

            Assert.HasCount(parallelTasks * batchSize * iterations, allIds, "Total inserted records mismatch");
            Assert.AreEqual(allIds.Count, allIds.Distinct().Count(), "Duplicate IDs found");
            Assert.IsTrue(allIds.All(id => id > 0), "All IDs should be positive");
        }

        private static async Task InsertDataInBatchesAsync(string batchId, int taskId, int batchSize, int iterations, bool retryOnLock = true)
        {
            using var queryProvider = _dbConnectionInfo.GetQueryProvider();

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                var valuesList = new List<string>();

                for (var i = 0; i < batchSize; i++)
                {
                    var value = $"Task{taskId}_Iter{iteration}_Row{i}_{Guid.NewGuid():N}";
                    valuesList.Add($"('{batchId}', {taskId}, '{value}')");
                }

                var insertSql = $"""
                                 INSERT INTO "{_dbConnectionInfo.DbName}"."{_dbConnectionInfo.Schema}"."{BenchMarkTableName}" ("BatchId", "TaskId", "Value")
                                 VALUES {string.Join(", ", valuesList)};
                                 """;

                if (retryOnLock)
                {
                    await ExecuteWithLockRetryAsync(queryProvider, insertSql, batchId).ConfigureAwait(false);
                }
                else
                {
                    await queryProvider.ExecuteAsync(insertSql).ConfigureAwait(false);
                }
            }
        }

        private static async Task ExecuteWithLockRetryAsync(QueryProvider queryProvider, string sql, string batchId)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await queryProvider.ExecuteAsync(sql).ConfigureAwait(false);
                    return;
                }
                catch (SnowflakeDbException ex) when (ex.ErrorCode == SnowflakeLockErrorCode && attempt < MaxRetries)
                {
                    LockRetries.AddOrUpdate(batchId, 1, (_, count) => count + 1);
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1) + Random.Shared.Next(0, 200))).ConfigureAwait(false);
                }
            }
        }

        private static async Task<List<long>> InsertAndReturnIdsAsync(string batchId, int taskId, int batchSize, int iterations)
        {
            var insertedIds = new List<long>();
            using var queryProvider = _dbConnectionInfo.GetQueryProvider();

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                var valuesList = new List<string>();

                for (var i = 0; i < batchSize; i++)
                {
                    var value = $"Task{taskId}_Iter{iteration}_Row{i}_{Guid.NewGuid():N}";
                    valuesList.Add($"('{batchId}', {taskId}, '{value}')");
                }

                var insertSql = $"""
                    INSERT INTO "{_dbConnectionInfo.DbName}"."{_dbConnectionInfo.Schema}"."{BenchMarkTableName}" ("BatchId", "TaskId", "Value")
                    VALUES {string.Join(", ", valuesList)};
                    """;

                await ExecuteWithLockRetryAsync(queryProvider, insertSql, batchId).ConfigureAwait(false);

                // Note: AUTOINCREMENT is not monotonic in Snowflake for parallel inserts,
                // therefore the IDs for the iteration are determined via the value prefix instead of ORDER BY Id DESC.
                var selectIdsSql = $"""
                    SELECT "Id" 
                    FROM "{_dbConnectionInfo.DbName}"."{_dbConnectionInfo.Schema}"."{BenchMarkTableName}"
                    WHERE "BatchId" = :BatchId AND "TaskId" = :TaskId AND "Value" LIKE :ValuePrefix
                    ORDER BY "Id";
                    """;

                var parameters = _dbConnectionInfo.CreateParameters();
                parameters.Add("BatchId", batchId);
                parameters.Add("TaskId", taskId);
                parameters.Add("ValuePrefix", $"Task{taskId}_Iter{iteration}_Row%");

                var ids = await queryProvider.QueryAsync<BenchmarkIdResult>(selectIdsSql, parameters).ConfigureAwait(false);
                insertedIds.AddRange(ids.Select(r => r.Id));
            }

            return insertedIds;
        }

        private static async Task VerifyUniquePrimaryKeysAsync(string batchId, int expectedCount)
        {
            var selectSql = $"""
                SELECT "Id", "BatchId", "TaskId", "Value"
                FROM "{_dbConnectionInfo.DbName}"."{_dbConnectionInfo.Schema}"."{BenchMarkTableName}"
                WHERE "BatchId" = :BatchId
                ORDER BY "Id";
                """;

            var parameters = _dbConnectionInfo.CreateParameters();
            parameters.Add("BatchId", batchId);

            using var queryProvider = _dbConnectionInfo.GetQueryProvider();
            var results = await queryProvider.QueryAsync<BenchmarkResult>(selectSql, parameters).ConfigureAwait(false);

            var resultList = results.ToList();

            Assert.HasCount(expectedCount, resultList, $"Expected {expectedCount} records, but found {resultList.Count}");

            var distinctIds = resultList.Select(r => r.Id).Distinct().ToList();
            Assert.HasCount(resultList.Count, distinctIds, "Duplicate primary keys found!");

            Assert.IsTrue(resultList.All(r => r.Id > 0), "All IDs should be positive");

            var orderedIds = resultList.Select(r => r.Id).ToList();
            Assert.IsTrue(orderedIds.SequenceEqual(orderedIds.OrderBy(id => id)), "IDs are not in sequential order");
        }

        private sealed record BenchmarkResult
        {
            public long Id { get; init; }
            public string BatchId { get; init; } = string.Empty;
            public int TaskId { get; init; }
            public string Value { get; init; } = string.Empty;
        }

        private sealed record BenchmarkIdResult
        {
            public long Id { get; init; }
        }
    }
}
