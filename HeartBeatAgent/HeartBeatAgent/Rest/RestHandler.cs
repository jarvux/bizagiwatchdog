using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Polly;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.Azure;

namespace HeartBeatAgent.Rest
{
    public interface IRestHandler
    {
        Task<HttpResponseMessage> Get(string host, string uri);
        Task Post(string queueName, string message);
    }

    public class RestHandler : IRestHandler
    {
        public async Task<HttpResponseMessage> Get(string host, string uri)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{host}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                var message = new HttpRequestMessage(HttpMethod.Get, uri);
                var response = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead);
                response.EnsureSuccessStatusCode();
                return response;
            }
        }

        public static Polly.Retry.RetryPolicy RetryPolicy()
        {
            return Policy
                .Handle<TaskCanceledException>()
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1));
        }

        public async Task Post(string queueName, string message)
        {
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            var cloudQueue = queueClient.GetQueueReference(queueName);
            await cloudQueue.CreateIfNotExistsAsync();
            var queueMsg = new CloudQueueMessage(message);
            await cloudQueue.AddMessageAsync(queueMsg);
        }
    }
}
