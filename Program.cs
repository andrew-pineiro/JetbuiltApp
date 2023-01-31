using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
//Returns ApiKey from App.config for specific vendor
string GetAPIKey(string vendor)
{
    string apiKey =
        ConfigurationManager.AppSettings[string.Format("{0}_apiKey", vendor.ToLower())] ?? string.Empty;
    return apiKey;
}
//Returns Base URI for API from App.config
Uri GetBaseURI()
{
    string baseURI =
        ConfigurationManager.AppSettings["baseURI"] ?? string.Empty;
    return new Uri(baseURI);
}
string GetOutputFile(string vendor, string fileName)
{
    string path =
        ConfigurationManager.AppSettings["productsFilePath"] ?? string.Empty;
    if (!string.IsNullOrEmpty(path))
    {
        return string.Format("{0}/{1}/{2}", path, vendor, fileName);
    }
    else { return path; }
}
HttpResponseMessage HttpSender(string apiKey, string method, string body, string endpoint)
{
    HttpClient sender = new HttpClient();
    sender.BaseAddress = GetBaseURI();
    sender.DefaultRequestHeaders.Accept.Clear();
    sender.DefaultRequestHeaders.Accept
        .Add(new MediaTypeWithQualityHeaderValue("application/vnd.jetbuilt.v1"));
    sender.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);
    HttpResponseMessage response = new();
    var _method = new HttpMethod(method);
    switch (_method.ToString().ToUpper())
    {
        case "GET":
            response = sender.GetAsync(endpoint).Result;
            break;
        case "POST":
            break;
        case "PUT":
            break;
        case "DELETE":
            break;
        default:
            throw new NotImplementedException();
    }
    return response;
}
void GetProducts(string apiKey, string vendor)
{
    string results = string.Empty;
    var initialResponse = HttpSender(apiKey, "GET", "", "/api/products");
    if (initialResponse.IsSuccessStatusCode)
    {
        int perPage = Int32.Parse(initialResponse.Headers.GetValues("x-per-page").First());
        int totalCount = Int32.Parse(initialResponse.Headers.GetValues("x-total-count").First());
        int totalPages = totalCount / perPage;
        for (int i = 1; i <= totalPages; i++)
        {
            var response = HttpSender(apiKey, "GET", "", $"/api/products?page={i}");
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
}
//void GetSqlResults(string vendor)
//{
//    string query = string.Format("exec sp_Jetbuilt_BuildFullProducts_ByVendor '{0}'", vendor);
//    string connString = ConfigurationManager.ConnectionStrings["staging"].ConnectionString;
//    SqlConnection conn = new SqlConnection(connString);
//    SqlCommand cmd = new SqlCommand(query, conn);
//    conn.Open();
//    SqlDataReader reader = cmd.ExecuteReader();
//    string output = string.Empty;
//    while(reader.Read())
//    {
//        output += reader[0].ToString() ?? string.Empty;
//    }
//    conn.Close();
//    File.WriteAllTextAsync(GetOutputFile(vendor, "ResponseProducts.json"), output);
//}
void RunCompareFiles(string vendor)
{
    //TODO - Get this working
    string scriptPath = ConfigurationManager.AppSettings["psScriptFilePath"] ?? string.Empty;
    string script = string.Format("{0}\\Jetbuilt_CompareProducts.ps1", scriptPath);
    if (!string.IsNullOrEmpty(scriptPath))
    {
        PowerShell ps = PowerShell.Create();
        ps.AddScript(File.ReadAllText(script));
        ps.AddArgument(vendor);
        ps.Invoke();

    }
    else { Console.WriteLine("Script path is empty."); }

}
void DeleteProducts(string apiKey, string vendor)
{
    string payload = string.Empty;
    var fileResult = File.ReadAllLines(GetOutputFile(vendor, "DeleteProducts.txt"));
    foreach (var id in fileResult)
    {
        if (!string.IsNullOrEmpty(id))
        {
            var response = HttpSender(apiKey, "DELETE", payload, $"/api/products/{id}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failure in DELETE request for ID: {id}");
            }
        }
    }
}
void AddProducts(string apiKey, string vendor)
{
    string payload = string.Empty;
    //var response = HttpSender(apiKey, "POST", payload, "/api/products");
}
void UpdateProducts(string apiKey, string vendor)
{
    string payload = string.Empty;
    //var response = HttpSender(apiKey, "PUT", payload, "/api/products");
}

List<string> vendors = new() { "CAMPLE", "SESCOM", "LAIRD", "MCS", "DELV", "OMX" };
foreach (string vendor in vendors)
{
    string apiKey = GetAPIKey(vendor);
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine(string.Format("ApiKey not found for {0}", vendor));
        continue;
    }
    //GetProducts(apiKey, vendor);
    //GetSqlResults(vendor);
    RunCompareFiles(vendor);
    //DeleteProducts(apiKey, vendor);
    //AddProducts(apiKey, vendor);
    //UpdateProducts(apiKey, vendor);
}