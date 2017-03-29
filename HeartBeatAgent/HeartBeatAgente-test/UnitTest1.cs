using System.Net.Http;
using HeartBeatAgent.Ping;
using HeartBeatAgent.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HeartBeatAgente_test
{
    [TestClass]
    public class TestPing
    {
        [TestMethod]
        public void TestSuccess()
        {
            var mock = new Mock<IRestHandler>();
            mock.Setup(x => x.Get("", "")).Throws(new HttpRequestException());
            var p = new PingDefault(new PingEnv("", "", "", "", "", 30, ""), mock.Object);
            var r = p.TryPingServer();
            Assert.AreEqual("failure", r.ToJson());
        }
    }
}