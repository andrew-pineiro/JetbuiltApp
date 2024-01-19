using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static JetbuiltApp.Data.HelperFunctions;

namespace JetbuiltApp.Data
{
    public class HttpSender
    {
        public HttpResponseMessage Send(string apiKey, string method, ProductModel? body, string endpoint)
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
                        new System.Text.Json.JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }
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

    }
}
