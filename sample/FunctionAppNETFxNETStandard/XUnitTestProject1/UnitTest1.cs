using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using FunctionAppNETFramework;
using Xunit;

namespace XUnitTestProject1
{
    public class NetProjectSystem_XunitTest
    {
        [Fact]
        public async Task XUnitTest_NETFx()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://functions.azurewebsites.net?name=test")
            {
                Content = new StringContent(""),
                Properties = { { HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration() } }
            };

            var response = await HttpTriggerNETFramework.Run(requestMessage, null);

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
