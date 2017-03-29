using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Supervision;
using HeartBeatAgent.Facts;
using HeartBeatAgent.Ping;
using HeartBeatAgent.Rest;
using HeartBeatAgent.scheduler;

namespace HeartBeatAgent
{
    public class HaertBeatAgentService
    {
        private ActorSystem HeartBeatAgent;
        public void Start()
        {
            var appSettings = ConfigurationManager.AppSettings;
            var pingHost = appSettings["ping.host"];
            var pingUri = appSettings["ping.uri"];
            var lapse = Convert.ToInt32(appSettings.Get("lapse"));
            var node = appSettings.Get("node");
            var env = appSettings.Get("environment");
            var enginequeue = appSettings.Get("QueueName");

            var restHandler = new RestHandler();

            var scheduler = new PingScheduler(
                new PingDefault(new PingEnv(pingHost, "/test", pingUri, env, node, lapse, "ENGINE"), restHandler),
                new FactRepository(restHandler, enginequeue)
            );
            
            HeartBeatAgent = ActorSystem.Create("HeartBeatAgent");
            var materializer = HeartBeatAgent.Materializer(ActorMaterializerSettings.Create(HeartBeatAgent).WithSupervisionStrategy(Deciders.RestartingDecider));
            scheduler.StartScheduler(lapse, materializer);
        }

        public async void Stop()
        {
            await HeartBeatAgent.Terminate();
        }
    }
}
