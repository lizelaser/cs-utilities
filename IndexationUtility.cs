using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lizelaser0310.Utilities
{
    public static class IndexationUtility
    {
        public static async Task<bool> updateDocument<T>(
            string indexUid,
            T payload,
            string masterKey,
            string meiliUrl = "http://localhost:7700"
        )
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(meiliUrl);
            client.DefaultRequestHeaders.Add("X-Meili-API-Key", masterKey);

            var listPayload = new T[] { payload };

            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonOptions.PropertyNameCaseInsensitive = true;
            var json = JsonSerializer.Serialize(listPayload, jsonOptions);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/indexes/{indexUid}/documents", content);

            return response.IsSuccessStatusCode;
        }
    }
}