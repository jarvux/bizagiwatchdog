using Topshelf;
using Topshelf.Runtime;

namespace HeartBeatAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(configure =>
            {
                configure.Service<HaertBeatAgentService>(service =>
                {
                    service.ConstructUsing(_ => new HaertBeatAgentService());
                    service.WhenStarted( _ => _.Start());
                    service.WhenStopped(_ => _.Stop());
                });
                
                configure.RunAsLocalSystem();
                configure.SetServiceName("StatusReportAgent");
                configure.SetDisplayName("StatusReportAgent");
                configure.StartAutomatically();
            });
        }
    }
}
