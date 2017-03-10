using System;
using HeartBeatAgent.scheduler;
using HeartBeatAgent.Ping;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Supervision;
using HeartBeatAgent.Facts;
using HeartBeatAgent.Rest;
using System.Configuration;

namespace HeartBeatAgent
{
    class Program
    {
        static void Main(string[] args)
        {

            var appSettings = ConfigurationManager.AppSettings;

            var pingHost = appSettings["ping.host"];
            var pingUri = appSettings["ping.uri"];

            var factHost = appSettings["fact.host"];
            var factUri = appSettings["fact.uri"];


            var lapse = Convert.ToDouble(appSettings.Get("lapse"));
            var node = appSettings.Get("node");
            var env = appSettings.Get("environment");

            IRestHandler restHandler = new RestHandler();

            var scheduler = new PingScheduler(
                new PingDefault(new PingEnv(pingHost, "/test", pingUri, env, node, lapse), restHandler),
                new FactRepository(restHandler, factHost, factUri)
            );


            using (var system = ActorSystem.Create("HeartBeatAgent"))
            {
                using (var materializer = system.Materializer(ActorMaterializerSettings.Create(system).WithSupervisionStrategy(Deciders.RestartingDecider)))
                {
                    var task = scheduler.StartScheduler(lapse, materializer);
                    task.Wait();
                }
            }
        }
    }
}
