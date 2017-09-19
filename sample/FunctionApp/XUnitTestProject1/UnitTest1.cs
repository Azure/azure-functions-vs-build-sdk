using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using FunctionApp;
using Xunit;

namespace XUnitTestProject1
{
    public class NetProjectSystem_XunitTest
    {
        [Fact]
        public async Task XUnitTest()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://functions.azurewebsites.net?name=test")
            {
                Content = new StringContent(""),
                Properties = { { HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration() } }
            };

            var response = await HttpTriggerCSharp.Run(requestMessage, null);

            string responseString = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("\"Hello test\"", responseString);
        }


        [Fact]
        public void NewProjectSystem_XUnit_Empty()
        {
            Assert.True(true);
        }
    }
}
