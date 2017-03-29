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

        public void StartScheduler(int lapse, IMaterializer materializer)
        {
            Source.Tick(TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(lapse), "")
            .Select(_ => ping.TryPingServer())
            .Select(r => r.ToJson())
            .RunWith(Sink.ForEach<string>(f => factRepository.SendFact(f)), materializer);
        }
    }
}
