using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.MSBuild.Tasks;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.MSBuild.Tests
{
    public class ZipDeploymentStatusTests
    {
        private static string UserAgentName = "functions-core-tools";
        private static string UserAgentVersion = "1.0";
        private static string userName = "deploymentUser";
        private static string password = "deploymentPassword";

        [Theory]
        [InlineData(HttpStatusCode.Forbidden, DeployStatus.Unknown)]
        [InlineData(HttpStatusCode.NotFound, DeployStatus.Unknown)]
        [InlineData(HttpStatusCode.RequestTimeout, DeployStatus.Unknown)]
        [InlineData(HttpStatusCode.InternalServerError, DeployStatus.Unknown)]
        public async Task PollDeploymentStatusTest_ForErrorResponses(HttpStatusCode responseStatusCode, DeployStatus expectedDeployStatus)
        {
            // Arrange
            string deployUrl = "https://sitename.scm.azurewebsites.net/DeploymentStatus?Id=knownId";
            Action<Mock<IHttpClient>, bool> verifyStep = (client, result) =>
            {
                client.Verify(c => c.GetAsync(
                It.Is<Uri>(uri => string.Equals(uri.AbsoluteUri, deployUrl, StringComparison.Ordinal))));
                Assert.Equal($"{UserAgentName}/{UserAgentVersion}", client.Object.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault());
                Assert.True(result);
            };


            Mock<IHttpClient> client = new Mock<IHttpClient>();

            HttpRequestMessage requestMessage = new HttpRequestMessage();
            client.Setup(x => x.DefaultRequestHeaders).Returns(requestMessage.Headers);
            client.Setup(c => c.GetAsync(new Uri(deployUrl, UriKind.RelativeOrAbsolute))).Returns(() =>
            {
                return Task.FromResult(new HttpResponseMessage(responseStatusCode));
            });

            ZipDeploymentStatus deploymentStatus = new ZipDeploymentStatus(client.Object, $"{UserAgentName}/{UserAgentVersion}", null, false);

            // Act
            var actualdeployStatus = await deploymentStatus.PollDeploymentStatus(deployUrl, userName, password);

            // Assert
            verifyStep(client, expectedDeployStatus == actualdeployStatus);
        }


        [Theory]
        [InlineData(HttpStatusCode.OK, DeployStatus.Success)]
        [InlineData(HttpStatusCode.Accepted, DeployStatus.Success)]
        [InlineData(HttpStatusCode.OK, DeployStatus.Failed)]
        [InlineData(HttpStatusCode.Accepted, DeployStatus.Failed)]
        [InlineData(HttpStatusCode.OK, DeployStatus.Unknown)]
        [InlineData(HttpStatusCode.Accepted, DeployStatus.Unknown)]
        public async Task PollDeploymentStatusTest_ForValidResponses(HttpStatusCode responseStatusCode, DeployStatus expectedDeployStatus)
        {
            // Arrange
            string deployUrl = "https://sitename.scm.azurewebsites.net/DeploymentStatus?Id=knownId";
            Action<Mock<IHttpClient>, bool> verifyStep = (client, result) =>
            {
                client.Verify(c => c.GetAsync(
                It.Is<Uri>(uri => string.Equals(uri.AbsoluteUri, deployUrl, StringComparison.Ordinal))));
                Assert.Equal($"{UserAgentName}/{UserAgentVersion}", client.Object.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault());
                Assert.True(result);
            };


            Mock<IHttpClient> client = new Mock<IHttpClient>();

            HttpRequestMessage requestMessage = new HttpRequestMessage();
            client.Setup(x => x.DefaultRequestHeaders).Returns(requestMessage.Headers);
            client.Setup(c => c.GetAsync(new Uri(deployUrl, UriKind.RelativeOrAbsolute))).Returns(() =>
            {
                string statusJson = JsonConvert.SerializeObject(new
                {
                    status = Enum.GetName(typeof(DeployStatus), expectedDeployStatus)
                }, Formatting.Indented);

                HttpContent httpContent = new StringContent(statusJson, Encoding.UTF8, "application/json");
                HttpResponseMessage responseMessage = new HttpResponseMessage(responseStatusCode)
                {
                    Content = httpContent
                };

                return Task.FromResult(responseMessage);
            });

            ZipDeploymentStatus deploymentStatus = new ZipDeploymentStatus(client.Object, $"{UserAgentName}/{UserAgentVersion}", null, false);

            // Act
            var actualdeployStatus = await deploymentStatus.PollDeploymentStatus(deployUrl, userName, password);

            // Assert
            verifyStep(client, expectedDeployStatus == actualdeployStatus);

        }
    }
}
