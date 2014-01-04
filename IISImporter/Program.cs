using System.Net;
using System.Net.Http.Headers;
using IISContracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace IISImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0] == "Import" && args.Count() == 3)
            {
                var workingDirectory = args[1];
                var postEndpoint = new Uri(args[2]);

                var client = new HttpClient();

                foreach (var logFilePath in Directory.GetFiles(workingDirectory))
                {
                    Console.WriteLine("Executing: " + logFilePath);

                    var items = new List<IISViewModel>();

                    var allLines = File.ReadAllLines(logFilePath);

                    foreach (var line in allLines.Where(a => a.StartsWith("#") == false))
                    {
                        items.Add(ParseLine(line));
                    }

                    foreach (var sequence in items.ChunksOf(1000))
                    {
                        var content = new StringContent(JsonConvert.SerializeObject(sequence));
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        var task = Task.Run(() => client.PostAsync(postEndpoint, content));
                        task.Wait();
                        if (task.Result.StatusCode != HttpStatusCode.OK)
                        {
                            Console.WriteLine(task.Result);
                            Console.WriteLine(Task.Run(() => task.Result.Content.ReadAsStringAsync()).Result);
                        }
                    }
                }
            }
            else if (args[0] == "Parse" && args.Count() == 2)
            {
                Console.WriteLine(JsonConvert.SerializeObject(ParseLine(args[1])));
            }
        }
        
        public static IISViewModel ParseLine(string line)
        {
            var elements = line.Split(' ').ToArray();
            return new IISViewModel
                {
                    Timestamp = DateTime.Parse(elements[0] + " " + elements[1], new CultureInfo("en-GB")),
                    IpAddress = elements[2],
                    Verb = elements[3],
                    Url = elements[4],
                    QueryString = elements[5] == "-" ? null : elements[5],
                    Port = elements[6],
                    UserName = elements[7] == "-" ? null : elements[7],
                    UserAgent = elements[9],
                    ResponseStatusCode = int.Parse(elements[10]),
                    ResponseStatusSubCode = int.Parse(elements[11]),
                    TimeTaken = elements[13]
                };
        }
    }

    //2013-06-26 21:11:01 10.1.0.71 GET /Dashboard/Organisation - 443 sysadmin 62.133.9.226 Mozilla/5.0+(Windows+NT+6.1)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/28.0.1500.52+Safari/537.36 500 0 0 1794
}
