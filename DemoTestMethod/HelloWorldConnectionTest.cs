using Snowflake.Data.Client;

namespace DemoTestMethod
{
    [TestClass]
    [TestCategory("Snowflake Connection")]
    public class HelloWorldConnectionTest : TestSetup
    {
        [TestMethod]
        public async Task Should_Connect_And_Say_Hello_World()
        {
            var connectionInfo = await GetSnowflakeConnection().ConfigureAwait(false);

            using var queryProvider = connectionInfo.GetQueryProvider();
            var greeting = await queryProvider.ExecuteScalarAsync<string>("SELECT 'Hello World from Snowflake!'").ConfigureAwait(false);

            Assert.AreEqual("Hello World from Snowflake!", greeting);
        }

        [TestMethod]
        public async Task Should_Use_DoD2026_Context()
        {
            var connectionInfo = await GetSnowflakeConnection().ConfigureAwait(false);

            using var connection = new SnowflakeDbConnection(connectionInfo.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT CURRENT_USER(), CURRENT_ROLE(), CURRENT_WAREHOUSE(), CURRENT_DATABASE(), CURRENT_SCHEMA(), CURRENT_VERSION()";

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            Assert.IsTrue(await reader.ReadAsync().ConfigureAwait(false));

            Assert.AreEqual("DOD2026_USER", reader.GetString(0));
            Assert.AreEqual("DOD_DEVELOPER", reader.GetString(1));
            Assert.AreEqual("DOD_WAREHOUSE", reader.GetString(2));
            Assert.AreEqual(connectionInfo.DbName, reader.GetString(3));
            Assert.AreEqual(connectionInfo.Schema, reader.GetString(4));

            Console.WriteLine($"Snowflake Version: {reader.GetString(5)}");
        }
    }
}
