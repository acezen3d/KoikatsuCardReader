using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace KoikatsuCardReader.Core
{
    /// <summary>
    /// JSON serialization for display and export (camelCase, indented, null fields kept).
    /// </summary>
    public static class JsonExporter
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Include,
        };

        public static string ToJson(object value) => JsonConvert.SerializeObject(value, Settings);

        /// <summary>Writes UTF-8 without BOM.</summary>
        public static void Save(string path, object value)
        {
            File.WriteAllText(path, ToJson(value), new UTF8Encoding(false));
        }
    }
}
