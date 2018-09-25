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
    }
}
