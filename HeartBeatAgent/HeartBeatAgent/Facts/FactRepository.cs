using HeartBeatAgent.Rest;

namespace HeartBeatAgent.Facts
{
    interface IFactRepository
    {
        void SendFact(string content);
    }

    class FactRepository: IFactRepository
    {
        private string host;
        private IRestHandler restHandler;
        private string uri;

        public FactRepository(IRestHandler restHandler, string host, string uri)
        {
            this.restHandler = restHandler;
            this.host = host;
            this.uri = uri;
        }

        public void SendFact(string content)
        {
            restHandler.Post(host, uri, content);
        }
    }
}
