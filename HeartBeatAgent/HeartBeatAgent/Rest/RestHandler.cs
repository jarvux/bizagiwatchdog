using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Polly;

namespace HeartBeatAgent.Rest
{
    public interface IRestHandler
    {
        Task<HttpResponseMessage> Get(string host, string uri);
        Task<HttpResponseMessage> Post(string host, string uri, string content);
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
        
        public async Task<HttpResponseMessage> Post(string host, string uri, string input)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{host}");

                var message = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = new StringContent(input, Encoding.UTF8, "application/json")
                };
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
    }
}
