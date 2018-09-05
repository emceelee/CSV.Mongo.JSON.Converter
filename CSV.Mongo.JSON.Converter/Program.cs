using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;

using Newtonsoft.Json.Linq;

namespace CSV.Mongo.JSON.Converter
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string dirCSV = dir + @"\CSV\";
            string dirJSON = dir + @"\JSON\";

            CreateMatchingDirectories(dirCSV, dirJSON);

            var files = System.IO.Directory.GetFiles(dirCSV, "*.csv", SearchOption.AllDirectories);

            foreach(string file in files)
            {
                string fileName = file.Substring(0,file.Length - 4);
                string fileExtension = file.Substring(file.Length - 4);
                string fileNameJson = fileName.Replace(dirCSV, dirJSON) + ".json";

                string fileNameCsvNoPath = file.Replace(dirCSV, "");
                string fileNameJsonNoPath = fileNameJson.Replace(dirJSON, "");

                if (fileExtension.ToLower().Equals(".csv"))
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    Console.WriteLine($"Processing: {fileNameCsvNoPath}");
                    file.Substring(file.Length - 4).ToUpper().Equals(".CSV");
                    using (var reader = new StreamReader(file))
                    {
                        using (var writer = new StreamWriter(fileNameJson))
                        {
                            string csv = reader.ReadToEnd();
                            string json = ProcessCsv(csv);
                            writer.WriteLine(json);
                        }
                    }

                    stopwatch.Stop();

                    var fileInfo = new FileInfo(fileNameJson);
                    var sizeKB = fileInfo.Length / 1024.0;
                    bool showMB = sizeKB > 1024;
                    var sizeMB = sizeKB / 1024.0;

                    string display = $"{sizeKB.ToString("0.00")} KB";
                    if (showMB)
                    {
                        display = $"{sizeMB.ToString("0.00")} MB";
                    }
                    Console.WriteLine($"Complete ({stopwatch.Elapsed.TotalSeconds.ToString("0.000")} s): {fileNameJsonNoPath} - {display}");
                }
;
            }
        }

        public static void CreateMatchingDirectories(string directorySource, string directoryTarget)
        {
            System.IO.Directory.CreateDirectory(directorySource);
            System.IO.Directory.CreateDirectory(directoryTarget);

            foreach (string dirSource in System.IO.Directory.GetDirectories(directorySource))
            {
                CreateMatchingDirectories(dirSource, dirSource.Replace(directorySource, directoryTarget));
            }
        }

        public static string ProcessCsv(string csvInput)
        {
            bool firstRecord = true;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[");

            using (var csvReader = new StringReader(csvInput))
            using (var parser = new CsvTextFieldParser(csvReader))
            {
                // Skip the header line
                if (!parser.EndOfData)
                {
                    var header = parser.ReadFields();

                    while (!parser.EndOfData)
                    {
                        if(firstRecord)
                        {
                            firstRecord = false;
                        }
                        else
                        {
                            sb.AppendLine(",");
                        }
                        var csvLine = parser.ReadFields();
                        var obj = CreateObject(header, csvLine) as IDictionary<string, object>;

                        var bson = obj.ToBsonDocument();
                        var json = bson.ToJson();

                        //Console.WriteLine(json);
                        var jsonPretty = ReplaceISODate(JsonPrettify(StripISODate(json)));
                        sb.AppendLine(jsonPretty);
                    }
                }
            }

            sb.AppendLine("]");
            return sb.ToString();
        }

        public static dynamic CreateObject(string[] header, string[] record)
        {
            var recordObject = new ExpandoObject() as IDictionary<string, object>;

            for (int i = 0; i < header.Length; ++i)
            {
                string headerValue = header[i];
                string recordValue = null;

                if (i < record.Length)
                {
                    recordValue = record[i];
                }

                if (!String.IsNullOrEmpty(recordValue))
                {
                    string[] splitHeaderValue = SplitHeader(headerValue);
                    string currentHeaderValue = splitHeaderValue[0];
                    string lastHeaderValue = splitHeaderValue[splitHeaderValue.Length - 1];
                    bool nestedObject = splitHeaderValue.Length > 1;
                    Object recordValueObject;

                    if (nestedObject)
                    {
                        dynamic nested;
                        recordObject.TryGetValue(currentHeaderValue, out nested);
                        recordObject[currentHeaderValue] = CreateNestedObject(splitHeaderValue.Skip(1).ToArray(), recordValue, nested);
                    }
                    else
                    {
                        recordValueObject = DetermineObjectValue(recordValue, lastHeaderValue);
                        recordObject[headerValue] = recordValueObject;
                    }
                }
            }
            
            return recordObject;
        }

        public static dynamic CreateNestedObject(string[] splitHeaderValue, string recordValue, dynamic nested)
        {
            dynamic returnObject;

            string currentHeaderValue = splitHeaderValue[0];
            string lastHeaderValue = splitHeaderValue[splitHeaderValue.Length - 1];
            bool nestedObject = splitHeaderValue.Length > 1;

            int currentHeaderValueInteger;
            bool enumerable = Int32.TryParse(currentHeaderValue, out currentHeaderValueInteger);

            if (nested == null)
            {
                if(enumerable)
                {
                    returnObject = new List<dynamic>();
                }
                else
                {
                    returnObject = new ExpandoObject();
                }
            }
            else
            {
                returnObject = nested;
            }

            //IEnumerable
            if(enumerable)
            {
                var listObject = (List<dynamic>) returnObject;
                dynamic currentObject = null;

                //update existing object in list
                if(currentHeaderValueInteger < listObject.Count())
                {
                    currentObject = listObject[currentHeaderValueInteger];
                    listObject[currentHeaderValueInteger] = CreateNestedObject(splitHeaderValue.Skip(1).ToArray(), recordValue, currentObject);
                }
                //Create new object
                else if(currentHeaderValueInteger == listObject.Count())
                {
                    if (nestedObject)
                    {
                        currentObject = CreateNestedObject(splitHeaderValue.Skip(1).ToArray(), recordValue, currentObject);
                    }
                    else
                    {
                        currentObject = DetermineObjectValue(recordValue, lastHeaderValue);
                    }
                    
                    listObject.Add(currentObject);
                }
                else 
                {
                    throw new ArgumentOutOfRangeException($"Failed to add index {currentHeaderValueInteger}.  Only {listObject.Count()} objects currently exist.");
                }                
            }
            //Property
            else
            {
                var expando = (IDictionary<string, object>)returnObject;

                if(nestedObject)
                {
                    dynamic nestedNext;
                    expando.TryGetValue(currentHeaderValue, out nestedNext);
                    expando[currentHeaderValue] = CreateNestedObject(splitHeaderValue.Skip(1).ToArray(), recordValue, nestedNext);
                }
                else
                {
                    Object recordValueObject = DetermineObjectValue(recordValue, lastHeaderValue);
                    expando[currentHeaderValue] = recordValueObject;
                }
            }
            return returnObject;
        }

        public static Object DetermineObjectValue(string recordValue, string lastHeaderValue)
        {

            Object recordValueObject = recordValue;
            DateTime recordValueDate;
            if (DateTime.TryParse(recordValue, out recordValueDate))
            {
                recordValueObject = DateTime.SpecifyKind(recordValueDate, DateTimeKind.Utc);
            }

            double recordValueDouble;
            if (Double.TryParse(recordValue, out recordValueDouble))
            {
                if (!lastHeaderValue.ToLower().Equals("description") &&
                    !lastHeaderValue.ToLower().Equals("_id") &&
                    !lastHeaderValue.ToLower().Equals("code")
                    )
                {
                    recordValueObject = recordValueDouble;
                }
            }

            bool recordValueBool;
            if (Boolean.TryParse(recordValue, out recordValueBool))
            {
                recordValueObject = recordValueBool;
            };

            return recordValueObject;
        }

        public static string[] SplitHeader(string header)
        {
            return header.Split('/');
        }

        public static string StripISODate(string json)
        {
            return json.Replace(@"ISODate(""", @"""ISODate(\""").Replace(@"Z"")", @"Z\"")""");
        }

        public static string JsonPrettify(string json)
        {
            return JToken.Parse(json).ToString();
        }

        public static string ReplaceISODate(string json)
        {
            return json.Replace(@"""ISODate(\""", @"ISODate(""").Replace(@"Z\"")""", @"Z"")");
        }
    }
}
