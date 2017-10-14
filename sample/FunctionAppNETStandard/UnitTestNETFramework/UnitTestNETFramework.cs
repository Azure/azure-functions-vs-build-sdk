using System;
using FunctionAppNETStandard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject2;

namespace UnitTestNETFramework
{
    [TestClass]
    public class UnitTestNETFramework
    {
        [TestMethod]
        public void NewProjectSystem_MsTest_NETFx()
        {
            HttpRequest httpRequest = new DefaultHttpRequest();
            var response = HttpTriggerNETStandard.Run(httpRequest, null);

            if (response is OkObjectResult)
            {
                Assert.AreEqual("Hello, test", ((OkObjectResult)response).Value);
            }

        }
    }
}
