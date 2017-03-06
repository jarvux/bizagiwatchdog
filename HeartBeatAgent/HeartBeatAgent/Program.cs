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

            var appSettings = ConfigurationSettings.AppSettings;

            var pingHost = appSettings["ping.host"];
            var pingUri = appSettings["ping.uri"];

            var factHost = appSettings["fact.host"];
            var factUri = appSettings["fact.uri"];

            IRestHandler restHandler = new RestHandler();

            PingScheduler scheduler = new PingScheduler(
                new PingDefault(new PingEnv(pingHost, "/test", pingUri), restHandler),
                new FactRepository(restHandler, factHost, factUri)
            );

            var seconds = TimeSpan.FromSeconds(30);

            using (var system = ActorSystem.Create("HeartBeatAgent"))
            {
                using (var materializer = system.Materializer(ActorMaterializerSettings.Create(system).WithSupervisionStrategy(Deciders.RestartingDecider)))
                {
                    var task = scheduler.StartScheduler(seconds, materializer);
                    task.Wait();
                }
            }
        }
    }
}
