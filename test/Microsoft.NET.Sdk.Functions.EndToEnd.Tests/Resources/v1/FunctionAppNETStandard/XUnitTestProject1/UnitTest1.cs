using FunctionAppNETStandard;
using FunctionsRefNETStandard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace XUnitTestProject1
{
    public class NetProjectSystem_XunitTest
    {

        [Fact]
        public void NewProjectSystem_Xunit_NETCore()
        {
            HttpRequest httpRequest = new UnitTestProject2.DefaultHttpRequest();
            var response = HttpTriggerNETStandard.Run(httpRequest, null);

            if (response is OkObjectResult)
            {
                Assert.Equal("Hello, test", ((OkObjectResult)response).Value);
            }

        }

        [Fact]
        public void NewProjectSystem_MsTest_NETFx_Ref()
        {
            HttpRequest httpRequest = new UnitTestProject2.DefaultHttpRequest();
            var response = HttpTriggerRefNETStandard.Run(httpRequest, null);

            if (response is OkObjectResult)
            {
                Assert.Equal("Hello, test", ((OkObjectResult)response).Value);
            }
        }
    }
}
