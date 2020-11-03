using System;
using Newtonsoft.Json;

namespace Core.Json.Newtonsoft
{
    public static class NewtonsoftJsonSerializer
    {
        public static T Deserialize<T>(string jsonString, JsonSerializerSettings settings = default)
        {
            settings = settings ?? new JsonSerializerSettings();
            return JsonConvert.DeserializeObject<T>(jsonString, settings);
        }

        public static object Deserialize(Type type, string jsonString, JsonSerializerSettings settings = default)
        {
            settings = settings ?? new JsonSerializerSettings();
            return JsonConvert.DeserializeObject(jsonString, type, settings);
        }

        public static string Serialize(object obj, JsonSerializerSettings settings = default)
        {
            settings = settings ?? new JsonSerializerSettings();
            return JsonConvert.SerializeObject(obj, settings);
        }

    }
}
