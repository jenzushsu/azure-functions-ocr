#r "Newtonsoft.Json"

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

public static void Run(string myQueueItem, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {myQueueItem}");

    SendQueue(myQueueItem);
}

public static async Task<string> SendQueue(string queueItem)
{
    using (var client = new HttpClient())
    {

        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        dictionary.Add("blob_name", queueItem);

        string json = JsonConvert.SerializeObject(dictionary);
        var requestData = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(String.Format(ConfigurationManager.AppSettings["LogicAppUri"]), requestData);
        var result = await response.Content.ReadAsStringAsync();

        return result;
    }
}
