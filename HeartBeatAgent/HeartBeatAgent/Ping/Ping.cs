using HeartBeatAgent.Rest;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using static LanguageExt.Prelude;

namespace HeartBeatAgent.Ping
{
    interface IPing
    {
        PingResult TryPingServer();
    }

    public class PingEnv
    {
        public PingEnv(string host, string project, string uri)
        {
            Host = host;
            Project = project;
            Uri = uri;
        }

        public string Host { private set; get; }
        public string Project { private set; get; }
        public string Uri { private set; get; }
    }

    public class PingDefault : IPing
    {
        private PingEnv env;
        private IRestHandler handler;

        public PingDefault(PingEnv env, IRestHandler handler)
        {
            this.env = env;
            this.handler = handler;
        }
        public PingResult TryPingServer()
        {
            return Try(() => RestHandler.RetryPolicy().ExecuteAsync(() => handler.Get(env.Host, env.Uri)))
                .Map(r => r.Result.Content.ReadAsStringAsync().Result)
                .Filter(r => r.Contains("ok"))
                .Match(Succ: ToPingResponse(), Fail: ToNetworkFailure());
        }

        private Func<string, PingResult> ToPingResponse()
        {
            return m => new PingSuccessResult();
        }

        private Func<Exception, PingResult> ToNetworkFailure()
        {
            return e => new PingErrorResult();
        }

    }

    public interface PingResult
    {
        string ToJson();
    }

    public class PingSuccessResult : PingResult
    {
        public string ToJson()
        {
            return "{\"status\":\"success\"}";
        }
    }

    public class PingErrorResult : PingResult
    {
        public string ToJson()
        {
            return "{\"status\":\"failure\"}";
        }
    }
}
