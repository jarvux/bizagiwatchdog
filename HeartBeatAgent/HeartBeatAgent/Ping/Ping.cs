using HeartBeatAgent.Rest;
using System;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace HeartBeatAgent.Ping
{
    internal interface IPing
    {
        PingResult TryPingServer();
    }

    public class PingEnv
    {
        public PingEnv(string host, string project, string uri, string environment, string node, int lapse, string component)
        {
            Host = host;
            Project = project;
            Uri = uri;
            Environment = environment;
            Node = node;
            Lapse = lapse;
            Component = component;
        }

        public string Host { private set; get; }
        public string Project { private set; get; }
        public string Uri { private set; get; }
        public string Node { set; get; }
        public string Environment { set; get; }
        public int Lapse { set; get; }
        public string Component { set; get; }
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
                .Filter(r => r.Contains("build"))
                .Match(Succ: ToPingResponse(), Fail: ToNetworkFailure());
        }
        private Func<string, PingResult> ToPingResponse()
        {
            return m => new PingSuccessResult(env);
        }
        private Func<Exception, PingResult> ToNetworkFailure()
        {
            return e => new PingErrorResult(env);
        }
    }

    public interface PingResult
    {
        string ToJson();
    }

    internal class PingSuccessResult : PingResult
    {
        private PingEnv _enviroment;
        internal PingSuccessResult(PingEnv env)
        {
            _enviroment = env;
        }
        public string ToJson()
        {
            var parameters = new
            {
                timestamp = DateTime.UtcNow.Ticks, 
                env = _enviroment.Environment,
                node = _enviroment.Node,
                status = true,
                statusCode = 200,
                lapse = _enviroment.Lapse,
                component = _enviroment.Component
            };

            return JsonConvert.SerializeObject(parameters);
        }
    }

    internal class PingErrorResult : PingResult
    {
        private PingEnv _enviroment;
        internal PingErrorResult(PingEnv env)
        {
            _enviroment = env;
        }
        public string ToJson()
        {
            var parameters = new
            {
                timestamp = DateTime.UtcNow.Ticks,
                env = _enviroment.Environment,
                node = _enviroment.Node,
                status = false,
                statusCode = 500,
                lapse = _enviroment.Lapse,
                component = _enviroment.Component
            };

            return JsonConvert.SerializeObject(parameters);
        }
    }
}
