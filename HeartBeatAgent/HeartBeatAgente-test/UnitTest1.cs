using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using HeartBeatAgent.Rest;
using HeartBeatAgent.Ping;
using System.Net.Http;

namespace HeartBeatAgente_test
{
    [TestClass]
    public class TestPing
    {
        [TestMethod]
        public void TestSuccess()
        {
            Mock<IRestHandler> mock = new Mock<IRestHandler>();
            mock.Setup(x => x.Get("", "")).Throws(new HttpRequestException());
            var p = new PingDefault(new PingEnv("", "", "","","",30,""), mock.Object);
            var r = p.TryPingServer();
            Assert.AreEqual("failure", r.ToJson());
        }
    }
}
