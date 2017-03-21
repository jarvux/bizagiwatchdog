using System.Threading.Tasks;
using HeartBeatAgent.Rest;

namespace HeartBeatAgent.Facts
{
    interface IFactRepository
    {
        Task SendFact(string content);
    }

    class FactRepository: IFactRepository
    {
        private string QueueName;
        private IRestHandler RestHandler;

        public FactRepository(IRestHandler restHandler, string queueName)
        {
            RestHandler = restHandler;
            QueueName = queueName;
        }

        public Task SendFact(string content)
        {
            return RestHandler.Post(QueueName, content);
        }
    }
}
