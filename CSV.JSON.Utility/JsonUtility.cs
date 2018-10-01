using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json.Linq;

namespace CSV.JSON.Utility
{
    public static class JsonUtility
    {
        public static string JsonPrettify(string json)
        {
            return JToken.Parse(json).ToString();
        }

        public static JArray DeserializeAsJArray(string json)
        {
            JArray array = null;

            string cleanJson = json.Trim();

            try
            {
                //handle input array
                if (cleanJson[0] == '[' && cleanJson[cleanJson.Length - 1] == ']')
                {
                    array = JArray.Parse(cleanJson);
                }
                else
                {
                    array = new JArray();
                    array.Add(JObject.Parse(cleanJson));
                }
            }
            catch(Exception ex)
            {
                array = new JArray();
            }

            return array;
        }
    }
}
