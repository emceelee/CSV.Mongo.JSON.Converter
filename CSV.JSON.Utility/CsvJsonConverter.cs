using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

using Newtonsoft.Json;

namespace CSV.JSON.Utility
{
    public static class CsvJsonConverter
    {
        public static string Convert(string[] header, string[] record, string[] stringProperties = null)
        {
            var obj = CsvUtility.CreateObject(header, record, stringProperties) as IDictionary<string, object>;

            var json = JsonConvert.SerializeObject(obj);

            var jsonPretty = JsonUtility.JsonPrettify(json);

            return jsonPretty;
        }
    }
}
