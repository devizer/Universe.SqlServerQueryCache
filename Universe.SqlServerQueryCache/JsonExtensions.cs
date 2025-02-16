using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Universe.SqlServerQueryCache
{
    public enum JsonNaming
    {
        CamelCase,
        PascalCase,
        KebabCase,
        SnakeCase,
    }

    public static class JsonExtensions
    {
        public static T CloneByJson<T>(this T arg)
        {
            if ((object)arg == null) return default(T);
            var json = arg.ToJsonString(minify: true, JsonNaming.PascalCase);
            return ParseJsonString<T>(json, JsonNaming.PascalCase);
        }

        public static string ToJsonString(this object arg, bool minify = false, JsonNaming namingStrategy = JsonNaming.CamelCase)
        {
            var ser = CreateJsonSerializer(minify, namingStrategy);
            StringBuilder json = new StringBuilder();
            StringWriter jwr = new StringWriter(json);
            ser.Serialize(jwr, arg);
            jwr.Flush();

            return json.ToString();
        }

        public static string ToJsonFile(string path, object arg, bool minify = false, JsonNaming namingStrategy = JsonNaming.CamelCase)
        {
            var ser = CreateJsonSerializer(minify, namingStrategy);
            try
            {
                var jsonString = arg.ToJsonString(minify, namingStrategy);
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter wr = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    wr.Write(jsonString);
                    return jsonString;
                }
                return jsonString;
                /*
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter wr = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    var jsonTextWriter = new JsonTextWriter(wr);
                    ser.Serialize(jsonTextWriter, arg);
                    jsonTextWriter.Flush();
                }
                */

            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to serialize object [{arg?.GetType()}] to json file \"{path}\". {ex.GetExceptionDigest()}", ex);
            }
        }

        public static T ParseJsonString<T>(string json, JsonNaming namingStrategy = JsonNaming.CamelCase)
        {
            var ser = CreateJsonSerializer(true, namingStrategy);
            JsonReader jsonReader = new JsonTextReader(new StringReader(json));
            return ser.Deserialize<T>(jsonReader);
        }

        public static T ParseJsonFile<T>(string fileName, JsonNaming namingStrategy = JsonNaming.CamelCase)
        {
            var ser = CreateJsonSerializer(true, namingStrategy);
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader rdr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    JsonReader jsonReader = new JsonTextReader(rdr);
                    return ser.Deserialize<T>(jsonReader);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to parse json file \"{fileName}\" as [{typeof(T)}]. {ex.GetExceptionDigest()}", ex);
            }
        }

        public static JsonSerializer CreateJsonSerializer(bool minify, JsonNaming namingStrategy) => new JsonSerializer()
        {
            Formatting = minify ?Formatting.None : Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = GetContractResolver(namingStrategy),
            MaxDepth = 32,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
        };

        public static IContractResolver GetContractResolver(JsonNaming jsonNamingStrategy)
        {
            switch (jsonNamingStrategy)
            {
                case JsonNaming.CamelCase: return CamelContractResolver;
                case JsonNaming.KebabCase: return KebabContractResolver;
                case JsonNaming.PascalCase: return PascalContractResolver;
                case JsonNaming.SnakeCase: return SnakeContractResolver;
                default:
                    throw new ArgumentException($"Unknown json naming strategy {jsonNamingStrategy}",
                        nameof(jsonNamingStrategy));
            }
        }


        public static readonly IContractResolver PascalContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new DefaultNamingStrategy() { ProcessDictionaryKeys = true, }
        };
        public static readonly IContractResolver CamelContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new DefaultNamingStrategy() { ProcessDictionaryKeys = true, }
        };
        public static readonly IContractResolver KebabContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new DefaultNamingStrategy() { ProcessDictionaryKeys = true, }
        };
        public static readonly IContractResolver SnakeContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new DefaultNamingStrategy() { ProcessDictionaryKeys = true, }
        };
    }
}
