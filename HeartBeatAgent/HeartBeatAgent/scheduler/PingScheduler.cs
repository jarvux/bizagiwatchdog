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

        public Task StartScheduler(double lapse, IMaterializer materializer)
        {
            return Source.Tick(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(lapse), "")
                .Select(_ => ping.TryPingServer())
                .Select(r => r.ToJson())
                .RunWith(Sink.ForEach<string>(f => factRepository.SendFact(f)), materializer);
        }
    }
}
