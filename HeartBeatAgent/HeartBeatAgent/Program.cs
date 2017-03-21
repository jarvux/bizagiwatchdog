using System;
using HeartBeatAgent.scheduler;
using HeartBeatAgent.Ping;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Supervision;
using HeartBeatAgent.Facts;
using HeartBeatAgent.Rest;
using System.Configuration;
using Microsoft.Azure;

namespace HeartBeatAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;

            var pingHost = appSettings["ping.host"];
            var pingUri = appSettings["ping.uri"];

            //in minutes - default 5m
            var lapse = Convert.ToInt32(appSettings.Get("lapse"));
            var node = appSettings.Get("node");
            var env = appSettings.Get("environment");
            var enginequeue = appSettings.Get("QueueName");

            var restHandler = new RestHandler();

            var scheduler = new PingScheduler(
                new PingDefault(new PingEnv(pingHost, "/test", pingUri, env, node, lapse, "ENGINE"), restHandler),
                new FactRepository(restHandler, enginequeue)
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
