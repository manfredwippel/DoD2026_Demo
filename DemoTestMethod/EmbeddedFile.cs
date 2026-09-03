using System.Reflection;

namespace DemoTestMethod
{
    public static class EmbeddedFile
    {
        public static string GetFileContentFrom(string relativeName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                                   .SingleOrDefault(n => n.EndsWith("." + relativeName, StringComparison.Ordinal))
                               ?? throw new FileNotFoundException($"Eingebettete Ressource '{relativeName}' nicht gefunden. Vorhanden: {string.Join(", ", assembly.GetManifestResourceNames())}");

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
