using System.Collections.Immutable;

namespace DemoTestMethod
{
    [TestClass]
    public class DataTypes_HybridTests : TestSetup
    {
        // see https://docs.snowflake.com/en/sql-reference/intro-summary-data-types        
        private const string HybridDataTypeTestTableName = "HybridTestTableWithAllPossibleDataTypes";

        private static SnowflakeConnection Connection { get; set; } = null!;

        [ClassInitialize]
        public static async Task ClassInitAsync(TestContext _)
        {
            Connection = await GetSnowflakeConnection().ConfigureAwait(false);

            using var connection = Connection.GetQueryProvider();

            var sql = $"""
                       CREATE OR REPLACE HYBRID TABLE "{Connection.DbName}"."{Connection.Schema}"."{HybridDataTypeTestTableName}"
                       (
                           "Id"                         BIGINT NOT Null IDENTITY,
                           "Number_18_3"                NUMBER(18, 3),
                           "Number_21_0"                NUMBER(21, 0),
                           "NumberCol"                  NUMBER,
                           "Decimal_16_2"               DECIMAL(16, 2),
                           "Decimal_18_0"               DECIMAL(18, 0),
                           "DecimalCol"                 DECIMAL,
                           "Numeric_15_3"               NUMERIC(15, 3),
                           "Numeric_18_0"               NUMERIC(18, 0),
                           "NumericCol"                 NUMERIC,
                           "IntCol"                     INT,
                           "IntegerCol"                 INTEGER,
                           "BigIntCol"                  BIGINT,
                           "SmallIntCol"                SMALLINT,
                           "TinyIntCol"                 TINYINT,
                           "ByteIntCol"                 BYTEINT,
                           "FloatCol"                   FLOAT,
                            "Float4Col"                  FLOAT4,
                            "Float8Col"                  FLOAT8,
                           "DoubleCol"                  DOUBLE,
                           "DoublePrecisionCol"         DOUBLE PRECISION,
                           "RealCol"                    REAL,
                           "VarcharCol"                 VARCHAR,
                           "Varchar_255"                VARCHAR(255),
                           "Varchar_50"                 VARCHAR(50),
                           "Varchar_50_UTF8_upper"      VARCHAR(50) COLLATE 'utf8-upper',
                           "Varchar_50_UTF8_lower"      VARCHAR(50) COLLATE 'utf8-lower',
                           "CharCol"                    CHAR,
                           "Char_5"                     CHAR(5),
                           "CharacterCol"               CHARACTER,
                           "Character_5"                CHARACTER(5),
                           "StringCol"                  STRING,
                           "String_255"                 STRING(255),
                           "String_50"                  STRING(50),
                           "TextCol"                    TEXT,
                           "Text_255"                   TEXT(255),
                           "Text_50"                    TEXT(50),
                            "BinaryCol"                  BINARY,
                            "Binary_128"                 BINARY(128),
                            "VarbinaryCol"               VARBINARY,
                            "Varbinary_128"              VARBINARY(128),
                           "BooleanCol"                 BOOLEAN,
                           "DateCol"                    DATE,
                           "DateTimeCol"                DATETIME,
                           "TimeCol"                    TIME,
                           "TimeStampCol"               TIMESTAMP,
                           "TimeStampWithLocalTimeZone" TIMESTAMP_LTZ,
                           "TimeStampNoTimeZone"        TIMESTAMP_NTZ,
                           "TimeStampTimeZone"          TIMESTAMP_TZ,
                           "VariantCol"                 VARIANT,
                           "ArrayCol"                   ARRAY,
                           "ObjectCol"                  OBJECT,     
                            "FileCol"                    FILE,
                            "GeographyCol"               GEOGRAPHY,
                            "GeometryCol"                GEOMETRY,
                            "Vector_Float_256"           VECTOR(FLOAT, 256),
                            "Vector_Int_16"              VECTOR(INT, 16),
                       
                           -- not supported for hybrid tables
                           -- "StructuredArrayCol"         ARRAY(NUMBER),
                           -- "StructuredObjectCol"        OBJECT("Name" VARCHAR, "Value" NUMBER),
                           -- "MapCol"                     MAP(VARCHAR, NUMBER),
                            -- "UuidCol"                    UUID,
                           -- "DecFloatCol"                DECFLOAT,

                           Constraint PK_{HybridDataTypeTestTableName} PRIMARY KEY ("Id")
                       );
                       """;

            await connection.ExecuteAsync(sql).ConfigureAwait(false);

        }

        [Ignore("Hybrid tables are not supported in trial accounts")]
        [TestMethod]
        public async Task Fetch_Table_Schema_From_Hybrid_Table()
        {
            await AssertTableSchemaAsync(HybridDataTypeTestTableName).ConfigureAwait(false);
        }

        private static async Task AssertTableSchemaAsync(string tableName)
        {
            var tableSchema = await DatabaseService.GetTableSchemaAsync(tableName, Connection).ConfigureAwait(false);
            var currentJson = tableSchema.ToJson();

            var currentResult = currentJson.FromJsonStringAs<SnowflakeTableSchema>();

            var expectedSchema = EmbeddedFile.GetFileContentFrom("Fixture.HybridTableSchemaDataTypesResultSnowflake.json").FromJsonStringAs<SnowflakeTableSchema>();

            var actualSchemaWithOrderedColumns = currentResult with
            {
                ColumnInfos = currentResult.ColumnInfos.OrderBy(c => c.Field).ToImmutableList()
            };

            var expectedSchemaWithOrderedColumns = currentResult with
            {
                ColumnInfos = expectedSchema.ColumnInfos.OrderBy(c => c.Field).ToImmutableList()
            };

            Assert.That.ObjectsAreEqual(expectedSchemaWithOrderedColumns, actualSchemaWithOrderedColumns);
        }

        [ClassCleanup]
        public static async Task ClassCleanupAsync()
        {
            using var connection = Connection.GetQueryProvider();

            var sql = $"""
                       DROP TABLE IF EXISTS "{Connection.DbName}"."{Connection.Schema}"."{HybridDataTypeTestTableName}";
                       """;

            await connection.ExecuteAsync(sql).ConfigureAwait(false);
        }
    }
}