namespace DemoTestMethod
{
    /// <summary>
    /// Base class for all Snowflake tests: loads the configuration once per test run
    /// and provides the connection information.
    /// </summary>
    [TestClass]
    public abstract class TestSetup
    {
        private static readonly Lazy<SnowflakeConnection> ConnectionInfo = new(() => SnowflakeConnection.From(SnowflakeSettings.Load()));

        protected static Task<SnowflakeConnection> GetSnowflakeConnection() => Task.FromResult(ConnectionInfo.Value);

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var info = ConnectionInfo.Value;
            context.WriteLine($"Snowflake target: {info.DbName}.{info.Schema}");
        }
    }
}
