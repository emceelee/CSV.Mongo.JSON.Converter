using System;
using System.Collections.Generic;
using System.Text;

using MongoDB.Bson;

namespace CSV.JSON.Utility
{
    public static class CsvMongoJsonConverter
    {
        public static string Convert(string[] header, string[] record, string[] stringProperties = null)
        {
            var obj = CsvUtility.CreateObject(header, record, stringProperties) as IDictionary<string, object>;

            var bson = obj.ToBsonDocument();
            var json = bson.ToJson();

            var jsonPretty = ReplaceISODate(JsonUtility.JsonPrettify(StripISODate(json)));

            return jsonPretty;
        }

        private static string StripISODate(string json)
        {
            return json.Replace(@"ISODate(""", @"""ISODate(\""").Replace(@"Z"")", @"Z\"")""");
        }

        private static string ReplaceISODate(string json)
        {
            return json.Replace(@"""ISODate(\""", @"ISODate(""").Replace(@"Z\"")""", @"Z"")");
        }

    }
}
