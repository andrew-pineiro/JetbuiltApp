using Newtonsoft.Json;
using System.Configuration;
using System.Management.Automation;
using static JetbuiltApp.Data.HelperFunctions;

namespace JetbuiltApp.Data
{
    public class JetbuiltFunctions
    {
        private readonly HttpSender HttpSender = new();
        public int GetProducts(string apiKey, string vendor)
        {
            int retryAttempts = 5;
            int sleepInterval = 5;
            var _httpSender = HttpSender;
            try
            {
                string results = string.Empty;
                var initialResponse = _httpSender.Send(apiKey, "GET", null, "/api/products");
                if (!initialResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{DateTime.Now}] Failure in initial Http GET Request. Status Code: {initialResponse.StatusCode}");
                    for (int i = 1; i <= retryAttempts; i++ )
                    {
                        Thread.Sleep(1000 * sleepInterval);
                        Console.WriteLine($"[{DateTime.Now}] Retrying initial Http GET request. Attempt #{i}");
                        initialResponse = _httpSender.Send(apiKey, "GET", null, "/api/products");
                        if(initialResponse.IsSuccessStatusCode)
                        {
                            break;
                        }
                        sleepInterval=+5;
                    }

                    if(!initialResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Initial GET Request Failure. Status Code: {initialResponse.StatusCode}");
                        return 1;
                    }
                }
                decimal perPage = decimal.Parse(initialResponse.Headers.GetValues("x-per-page").First());
                decimal totalCount = decimal.Parse(initialResponse.Headers.GetValues("x-total-count").First());
                decimal totalPages = Math.Ceiling(totalCount / perPage);

                Console.WriteLine($"[{DateTime.Now}] Running GET Products for {vendor}; Pages: {totalPages}");

                for (int i = 1; i <= totalPages; i++)
                {
                    var response = _httpSender.Send(apiKey, "GET", null, $"/api/products?page={i}");
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Failure in looped Http Request at page #{i}");
                    }
                    var r = response.Content.ReadAsStringAsync().Result;
                    results += r[1..r.LastIndexOf(']')];
                    results += ",";
                }
                results = string.Format("[{0}]", results[..^1]);
                File.WriteAllTextAsync(GetOutputFile(vendor, "JetbuiltProducts.json"), results);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {e}");
                return 1;
            }

        }
        public static int RunCompareFiles(string vendor)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now}] Running Compare Files for {vendor}");
                string scriptPath = ConfigurationManager.AppSettings["psScriptFilePath"] ?? string.Empty;
                string scriptName = ConfigurationManager.AppSettings["psScriptFileName"] ?? string.Empty;
                string script = string.Format("{0}\\{1} '{2}'", scriptPath, scriptName, vendor);
                if (string.IsNullOrEmpty(scriptPath) || string.IsNullOrEmpty(scriptName))
                {
                    Console.WriteLine($"[{DateTime.Now}] Script {scriptName} not found in {scriptPath}");
                    return 2;
                }
                PowerShell ps = PowerShell.Create()
                    .AddScript($"powershell {script}");
                ps.Invoke();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {e}");
                return 1;
            }
        }
        public int DeleteProducts(string apiKey, string vendor)
        {
            var _httpSender = HttpSender;
            try
            {
                int errorCount = 0;
                var fileResult = File.ReadAllLines(GetOutputFile(vendor, "DeleteProducts.txt"));
                if (fileResult.Length <= 0)
                {
                    Console.WriteLine($"[{DateTime.Now}] {vendor} DELETE file empty; SKIPPED");
                    return 0;
                }
                Console.WriteLine($"[{DateTime.Now}] Running DELETE Products for {vendor}; Count: {fileResult.Length}");
                foreach (var id in fileResult)
                {
                    if (string.IsNullOrEmpty(id) && !int.TryParse(id, out int i))
                    {
                        Console.WriteLine($"[{DateTime.Now}] Invalid ID: {id}");
                        continue;
                    }
                    var response = _httpSender.Send(apiKey, "DELETE", null, $"/api/products/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Failure in DELETE request for ID: {id}");
                        errorCount++;
                        if(errorCount >= 5)
                        {
                            return 1;
                        }
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {e}");
                return 1;
            }

        }
        public int AddProducts(string apiKey, string vendor)
        {
            var _httpSender = HttpSender;
            try
            {
                string fileData = File.ReadAllText(GetOutputFile(vendor, "AddProducts.json"));
                int errorCount = 0;
                var payloads = JsonConvert.DeserializeObject<List<ProductModel>>(fileData);
                var jbData = JsonConvert.DeserializeObject<List<ProductModel>>(File.ReadAllText(GetOutputFile(vendor, "JetbuiltProducts.json")));

                if (payloads!.Count <= 0)
                {
                    Console.WriteLine($"[{DateTime.Now}] {vendor} POST file empty; SKIPPED");
                    return 0;
                }
                Console.WriteLine($"[{DateTime.Now}] Running POST Products for {vendor}; Count: {payloads!.Count}");
                foreach (var payload in payloads)
                {
                    string? id = jbData?.Find(x => x.model == payload.model)?.id;
                    if (!string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    var response = _httpSender.Send(apiKey, "POST", payload, "/api/products");
                    if (!response.IsSuccessStatusCode)
                    {
                        errorCount++;
                        Console.WriteLine($"[{DateTime.Now}] Error in POST request for {payload.model}");
                        if(errorCount >= 5)
                        {
                            return 1;
                        }
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {e}");
                return 1;
            }

        }
        public int UpdateProducts(string apiKey, string vendor)
        {
            var _httpSender = HttpSender;
            try
            {
                string fileData = File.ReadAllText(GetOutputFile(vendor, "UpdateProducts.json"));
                int errorCount = 0;
                var payloads = JsonConvert.DeserializeObject<List<ProductModel>>(fileData);
                var jbData = JsonConvert.DeserializeObject<List<ProductModel>>(File.ReadAllText(GetOutputFile(vendor, "JetbuiltProducts.json")));
                
                if (payloads!.Count <= 0)
                {
                    Console.WriteLine($"[{DateTime.Now}] {vendor} PUT file empty; SKIPPED");
                    return 0;
                }
                Console.WriteLine($"[{DateTime.Now}] Running PUT Products for {vendor}; Count: {payloads!.Count}");
                foreach (var payload in payloads)
                {
                    string? id = jbData?.Find(x => x.model == payload.model)?.id;
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    payload.discontinued = false;
                    var response = _httpSender.Send(apiKey, "PUT", payload, $"/api/products/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        errorCount++;
                        Console.WriteLine($"[{DateTime.Now}] Failure in PUT request for {id}");
                        if(errorCount >= 5)
                        {
                            return 1;
                        }
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {e}");
                return 1;
            }

        }
    }
}
