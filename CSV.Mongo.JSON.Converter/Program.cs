using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MongoDB.Bson;

using Newtonsoft.Json.Linq;

using CSV.JSON.Utility;

namespace CSV.Mongo.JSON.Converter
{
    class Program
    {
        private static string[] _stringProperties;

        static void Main(string[] args)
        {
            InitializeSettings();

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
            }
        }

        public static void InitializeSettings()
        {
            var stringProperties = System.Configuration.ConfigurationManager.AppSettings["stringProperties"];
            _stringProperties = stringProperties.Split(',').Select(s => s.ToLower()).ToArray();
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

                        var json = CsvMongoJsonConverter.Convert(header, csvLine, _stringProperties);
                        sb.AppendLine(json);
                    }
                }
            }

            sb.AppendLine("]");
            return sb.ToString();
        }
    }
}
