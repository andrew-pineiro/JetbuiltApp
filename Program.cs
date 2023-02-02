using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

string GetAPIKey(string vendor)
{
    string apiKey =
        ConfigurationManager.AppSettings[string.Format("{0}_apiKey", vendor.ToLower())] ?? string.Empty;
    return apiKey;
}
Uri GetBaseURI()
{
    string baseURI =
        ConfigurationManager.AppSettings["baseURI"] ?? string.Empty;
    if(string.IsNullOrEmpty(baseURI))
    {
        Console.WriteLine("Base URI Not Found.");
    }
    return new Uri(baseURI);
}
string GetOutputFile(string vendor, string fileName)
{
    string path =
        ConfigurationManager.AppSettings["productsFilePath"] ?? string.Empty;
    if (!string.IsNullOrEmpty(path))
    {
        return string.Format("{0}\\{1}\\{2}", path, vendor, fileName);
    }
    else 
    { 
        Console.WriteLine($"Output path does not exist. {vendor}\\{fileName}"); 
        return path; 
    }
}
HttpResponseMessage HttpSender(string apiKey, string method, Product? body, string endpoint)
{
    HttpClient sender = new HttpClient();
    sender.BaseAddress = GetBaseURI();
    sender.DefaultRequestHeaders.Accept.Clear();
    sender.DefaultRequestHeaders.Accept
        .Add(new MediaTypeWithQualityHeaderValue("application/vnd.jetbuilt.v1"));
    sender.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);
    HttpResponseMessage response;
    var _method = new HttpMethod(method);
    switch (_method.ToString().ToUpper())
    {
        case "GET":
            response = sender.GetAsync(endpoint).Result;
            break;
        case "POST":
            response = sender.PostAsJsonAsync(endpoint, body, 
                new System.Text.Json.JsonSerializerOptions {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }
                ).Result;
            break;
        case "PUT":
            response = sender.PutAsJsonAsync(endpoint, body,
                new System.Text.Json.JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }
                ).Result;
            break;
        case "DELETE":
            response = sender.DeleteAsync(endpoint).Result;
            break;
        default: throw new NotImplementedException();
    }
    return response;
}
void GetProducts(string apiKey, string vendor)
{
    try
    {
        Console.WriteLine($"Running GET Products for {vendor}");
        string results = string.Empty;
        var initialResponse = HttpSender(apiKey, "GET", null, "/api/products");
        if (initialResponse.IsSuccessStatusCode)
        {
            decimal perPage = decimal.Parse(initialResponse.Headers.GetValues("x-per-page").First());
            decimal totalCount = decimal.Parse(initialResponse.Headers.GetValues("x-total-count").First());
            decimal totalPages = Math.Ceiling(totalCount / perPage);
            Console.WriteLine($"Page Count: {totalPages}");
            for (int i = 1; i <= totalPages; i++)
            {
                var response = HttpSender(apiKey, "GET", null, $"/api/products?page={i}");
                if (response.IsSuccessStatusCode)
                {
                    var r = response.Content.ReadAsStringAsync().Result;
                    results += r.Substring(1, r.LastIndexOf(']') - 1);
                    results += ",";

                }
                else { Console.WriteLine(string.Format("Failure in looped Http Request at page #{0}", i)); }
            }
            results = string.Format("[{0}]", results.Substring(0, results.Length - 1));
            try { File.WriteAllTextAsync(GetOutputFile(vendor, "JBProducts.json"), results); }
            catch { throw new Exception("Failure in save file"); }
        }
        else
        {
            Console.WriteLine(
                string.Format("Failure in initial Http Request. Code {0}",
                initialResponse.StatusCode));
            throw new Exception("Failure in GET request");
        }

    } catch (Exception e) { Console.WriteLine(e); }
    
}
void RunCompareFiles(string vendor)
{
    try
    {
        Console.WriteLine($"Running Compare Files for {vendor}");
        string scriptPath = ConfigurationManager.AppSettings["psScriptFilePath"] ?? string.Empty;
        string script = string.Format("{0}\\Jetbuilt_CompareProducts.ps1 '{1}'", scriptPath, vendor);
        if (!string.IsNullOrEmpty(scriptPath))
        {
            PowerShell ps = PowerShell.Create();
            ps.AddScript($"powershell {script}");
            ps.Invoke();
        }
        else { Console.WriteLine("Script path is empty."); }
    } catch (Exception e) { Console.WriteLine(e); }
}
void DeleteProducts(string apiKey, string vendor)
{
    try
    {
        Console.WriteLine($"Running DELETE Products for {vendor}");
        var fileResult = File.ReadAllLines(GetOutputFile(vendor, "DeleteProducts.txt"));
        foreach (var id in fileResult)
        {
            if (!string.IsNullOrEmpty(id) && int.TryParse(id, out int i))
            {
                var response = HttpSender(apiKey, "DELETE", null, $"/api/products/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failure in DELETE request for ID: {id}");
                }
            }
        }
    } catch (Exception e) { Console.WriteLine(e); }
    
}
void AddProducts(string apiKey, string vendor)
{
    try
    {
        Console.WriteLine($"Running POST Products for {vendor}");
        string fileData = File.ReadAllText(GetOutputFile(vendor, "AddProducts.json"));
        if (fileData.Length > 0)
        {
            var payloads = JsonConvert.DeserializeObject<List<Product>>(fileData);

            if (payloads!.Count > 0)
            {
                foreach (var payload in payloads)
                {
                    var response = HttpSender(apiKey, "POST", payload, "/api/products");
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception();
                    }
                }
            }
        }
    } catch (Exception e) { Console.WriteLine(e); }

}
void UpdateProducts(string apiKey, string vendor)
{
    try
    {
        Console.WriteLine($"Running PUT Products for {vendor}");
        string fileData = File.ReadAllText(GetOutputFile(vendor, "UpdateProducts.json"));
        if (fileData.Length > 0)
        {
            var payloads = JsonConvert.DeserializeObject<List<Product>>(fileData);
            var jbData = JsonConvert.DeserializeObject<List<Product>>(File.ReadAllText(GetOutputFile(vendor, "JBProducts.json")));
            if (payloads!.Count > 0)
            {
                foreach (var payload in payloads)
                {
                    var id = jbData!.Find(x => x.model == payload.model)!.id;
                    var response = HttpSender(apiKey, "PUT", payload, $"/api/products/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception();
                    }
                }
            }
        }
    } catch (Exception e) { Console.WriteLine(e); }
    
}

List<string> vendors = new() { "CAMPLE", "SESCOM", "LAIRD", "MCS", "DELV", "OMX" };
foreach (string vendor in vendors)
{
    Console.WriteLine($"Beginning Process for {vendor}");
    string apiKey = GetAPIKey(vendor);
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine($"ApiKey not found for {vendor}, skipping vendor.");
        continue;
    }
    GetProducts(apiKey, vendor);
    RunCompareFiles(vendor);
    DeleteProducts(apiKey, vendor);
    AddProducts(apiKey, vendor);
    UpdateProducts(apiKey, vendor);
    Console.WriteLine("Complete.");
}
class Product
{
    public string? id { get; set; }
    public string? manufacturer { get; set; }
    public string? model { get; set; }
    public string? category_name { get; set; }
    public string? short_description { get; set; }
    public string? long_description { get; set; }
    public string? msrp { get; set; }
    public string? mapp { get; set; }
    public string? part_number { get; set; }
    public string? lead_time { get; set; }
    public string? image_url { get; set; }
}