using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HashServer
{
    public class WebAPI
    {
        static readonly HttpClient client;

        static WebAPI()
        {
            client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            client.MaxResponseContentBufferSize = 1024 * 1024;
            client.BaseAddress = new Uri(Program.Settings.External.gRoot);
        }

        public async static Task<HttpResponseMessage> POST(string data, string uri = "https://pdb2json.azurewebsites.net/api/PageHash/x")
        {
            var response = await client.PostAsync(uri, new StringContent(data, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            return response;
        }

        public async static Task ProxyObject(string data, Stream writeTo, string uri = "https://pdb2json.azurewebsites.net/api/PageHash/x")
        {
            var response = await client.PostAsync(uri, new StringContent(data, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            await response.Content.CopyToAsync(writeTo);
        }
    }
}
