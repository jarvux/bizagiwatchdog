using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Polly;
using Polly.Retry;
using System.Net.Http.Headers;

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

        public async Task Post(string queueName, string message)
        {
            var storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            var cloudQueue = queueClient.GetQueueReference(queueName);
            await cloudQueue.CreateIfNotExistsAsync();
            var queueMsg = new CloudQueueMessage(message);
            await cloudQueue.AddMessageAsync(queueMsg);
        }

        public static RetryPolicy RetryPolicy()
        {
            return Policy
                .Handle<TaskCanceledException>()
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1));
        }
    }
}