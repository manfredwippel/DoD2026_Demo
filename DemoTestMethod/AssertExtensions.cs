namespace DemoTestMethod
{
    public static class AssertExtensions
    {
        /// <summary>
        /// Compares two objects via their JSON representation.
        /// </summary>
        public static void ObjectsAreEqual<T>(this Assert _, T expected, T actual)
        {
            var expectedJson = expected.ToJson();
            var actualJson = actual.ToJson();

            Assert.AreEqual(expectedJson, actualJson, $"Objects differ.{Environment.NewLine}Expected:{Environment.NewLine}{expectedJson}{Environment.NewLine}Actual:{Environment.NewLine}{actualJson}");
        }

        /// <summary>
        /// Compares an object with an embedded JSON snapshot (for example, "Results.Xyz.json").
        /// </summary>
        public static void ObjectsAreEqual<T>(this Assert assert, string embeddedResultFile, T actual)
        {
            var expected = EmbeddedFile.GetFileContentFrom(embeddedResultFile).FromJsonStringAs<T>();
            assert.ObjectsAreEqual(expected, actual);
        }
    }
}
