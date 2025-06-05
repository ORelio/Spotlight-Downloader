using System.Text.Json;

namespace SpotlightDownloader
{
    internal static class Json
    {
        /// <summary>
        /// Parse some JSON and return the corresponding JsonElement.
        /// </summary>
        public static JsonElement ParseJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone(); // Clone to allow use after document is disposed
        }

        /// <summary>
        /// Parse a nested JSON string (e.g., the "item" property in Spotlight API).
        /// </summary>
        public static JsonElement ParseNestedJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }
}
