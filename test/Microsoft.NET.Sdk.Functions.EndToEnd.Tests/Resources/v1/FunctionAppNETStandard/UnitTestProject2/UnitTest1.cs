using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using FunctionAppNETStandard;
using FunctionsRefNETStandard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject2
{
    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void NewProjectSystem_MsTest_NETCore()
        {
            HttpRequest httpRequest = new DefaultHttpRequest();
            var response = HttpTriggerNETStandard.Run(httpRequest, null);

            if (response is OkObjectResult)
            {
                Assert.AreEqual("Hello, test", ((OkObjectResult)response).Value);
            }

        }

        [TestMethod]
        public void NewProjectSystem_MsTest_NETFx_Ref()
        {
            HttpRequest httpRequest = new DefaultHttpRequest();
            var response = HttpTriggerRefNETStandard.Run(httpRequest, null);

            if (response is OkObjectResult)
            {
                Assert.AreEqual("Hello, test", ((OkObjectResult)response).Value);
            }
        }
    }
}
