using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Streams;
using Akka.Streams.Dsl;
using HeartBeatAgent.Ping;
using HeartBeatAgent.Facts;

namespace HeartBeatAgent.scheduler
{

    class PingScheduler
    {
        private IPing ping;
        private IFactRepository factRepository;

        public PingScheduler(IPing ping, IFactRepository factRepository)
        {
            this.ping = ping;
            this.factRepository = factRepository;
        }

        public Task StartScheduler(TimeSpan time, IMaterializer materializer)
        {
            var result = Source.Tick(TimeSpan.FromSeconds(0), time, "")
                .Select(_ => ping.TryPingServer())
                .Select(r => r.ToJson())
                //.RunWith(Sink.ForEach<string>(f => factRepository.SendFact(f)), materializer);
                .RunWith(Sink.ForEach<string>(Console.WriteLine), materializer);

            return result;
        }
    }
}
