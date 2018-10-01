using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Reflection;

using CSV.JSON.Utility;

using Newtonsoft.Json.Linq;

namespace Mongo.JSON.CSV.Converter
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string dirCSV = dir + @"\CSV\";
            string dirJSON = dir + @"\JSON\";

            CreateMatchingDirectories(dirJSON, dirCSV);

            var files = System.IO.Directory.GetFiles(dirJSON, "*.json", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string fileName = file.Substring(0, file.Length - 5);
                string fileExtension = file.Substring(file.Length - 5);
                string fileNameCsv = fileName.Replace(dirJSON, dirCSV) + ".csv";

                string fileNameCsvNoPath = fileNameCsv.Replace(dirCSV, "");
                string fileNameJsonNoPath = file.Replace(dirJSON, "");

                if (fileExtension.ToLower().Equals(".json"))
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    Console.WriteLine($"Processing: {fileNameJsonNoPath}");

                    using (var reader = new StreamReader(file))
                    {
                        using (var writer = new StreamWriter(fileNameCsv))
                        {
                            string json = reader.ReadToEnd();

                            JArray obj = JsonUtility.DeserializeAsJArray(json);

                            List<string> header = null;
                            List<List<string>> records = null;

                            CsvUtility.CreateCsvFromJArray(obj, ref header, ref records);

                            var line = String.Join(',', header);
                            writer.WriteLine(line);

                            foreach(List<string> record in records)
                            {
                                line = String.Join(',', record);
                                writer.WriteLine(line);
                            }
                        }
                    }

                    stopwatch.Stop();

                    var fileInfo = new FileInfo(fileNameCsv);
                    var sizeKB = fileInfo.Length / 1024.0;
                    bool showMB = sizeKB > 1024;
                    var sizeMB = sizeKB / 1024.0;

                    string display = $"{sizeKB.ToString("0.00")} KB";
                    if (showMB)
                    {
                        display = $"{sizeMB.ToString("0.00")} MB";
                    }
                    Console.WriteLine($"Complete ({stopwatch.Elapsed.TotalSeconds.ToString("0.000")} s): {fileNameCsvNoPath} - {display}");
                }
            }

            Console.WriteLine("Press [enter] to continue.");
            Console.ReadLine();
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
    }
}
