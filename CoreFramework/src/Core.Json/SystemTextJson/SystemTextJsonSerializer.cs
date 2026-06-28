using System;
using System.Text.Json;

namespace Core.Json.SystemTextJson
{
    public static class SystemTextJsonSerializer
    {
        private static readonly JsonSerializerOptions DefaultOptions = CreateSerializerOptions();
        private static readonly JsonSerializerOptions CamelCaseOptions = CreateSerializerOptions(camelCase: true);
        private static readonly JsonSerializerOptions IndentedOptions = CreateSerializerOptions(indented: true);
        private static readonly JsonSerializerOptions CamelCaseIndentedOptions = CreateSerializerOptions(camelCase: true, indented: true);

        public static T ToObject<T>(this string jsonString, bool camelCase = true)
        {
            return jsonString == null 
                ? default 
                : JsonSerializer.Deserialize<T>(jsonString, GetSerializerOptions(camelCase));
        }

        public static object ToObject(this string jsonString, Type type, bool camelCase = true)
        {
            return jsonString == null 
                ? null 
                : JsonSerializer.Deserialize(jsonString, type, GetSerializerOptions(camelCase));
        }

        public static string ToJson(this object obj, bool camelCase = true, bool indented = false)
        {
            return obj == null
                ? null 
                : JsonSerializer.Serialize(obj, obj.GetType(), GetSerializerOptions(camelCase, indented));
        }

        private static JsonSerializerOptions GetSerializerOptions(bool camelCase = true, bool indented = false)
        {
            if (camelCase)
            {
                return indented ? CamelCaseIndentedOptions : CamelCaseOptions;
            }

            return indented ? IndentedOptions : DefaultOptions;
        }

        private static JsonSerializerOptions CreateSerializerOptions(bool camelCase = false, bool indented = false)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = indented
            };

            if (!camelCase) 
                return options;

            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

            return options;
        }
    }
}
