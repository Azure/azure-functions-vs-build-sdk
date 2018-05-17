using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.Tasks;
using Moq;
using Xunit;

namespace ZipDeployPublish.Test
{
    public class ZipDeployerTaskTests
    {
        private static string _testZippedPublishContentsPath;
        private static string TestAssemblyToTestZipPath = @"Resources\TestPublishContents.zip";

        public static string TestZippedPublishContentsPath
        {
            get
            {
                if (_testZippedPublishContentsPath == null)
                {
                    string codebase = typeof(ZipDeployerTaskTests).Assembly.CodeBase;
                    string assemblyPath = new Uri(codebase, UriKind.Absolute).LocalPath;
                    string baseDirectory = Path.GetDirectoryName(assemblyPath);
                    _testZippedPublishContentsPath = Path.Combine(baseDirectory, TestAssemblyToTestZipPath);
                }

                return _testZippedPublishContentsPath;
            }
        }

        [Fact]
        public async Task ExecuteZipDeploy_InvalidZipFilePath()
        {
            Mock<IHttpClient> client = new Mock<IHttpClient>();
            ZipDeployTask zipDeployer = new ZipDeployTask();

            bool result = await zipDeployer.ZipDeployAsync(string.Empty, "username", "password", "siteName", client.Object);

            client.Verify(c => c.PostAsync(It.IsAny<Uri>(), It.IsAny<StreamContent>()), Times.Never);
            Assert.False(result);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.Accepted, true)]
        [InlineData(HttpStatusCode.Forbidden, false)]
        [InlineData(HttpStatusCode.NotFound, false)]
        [InlineData(HttpStatusCode.RequestTimeout, false)]
        [InlineData(HttpStatusCode.InternalServerError, false)]
        public async Task ExecuteZipDeploy_VaryingHttpResponseStatuses(HttpStatusCode responseStatusCode, bool expectedResult)
        {
            Mock<IHttpClient> client = new Mock<IHttpClient>();

            //constructing HttpRequestMessage to get HttpRequestHeaders as HttpRequestHeaders contains no public constructors
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            client.Setup(x => x.DefaultRequestHeaders).Returns(requestMessage.Headers);
            client.Setup(c => c.PostAsync(It.IsAny<Uri>(), It.IsAny<StreamContent>())).Returns((Uri uri, StreamContent streamContent) =>
            {
                byte[] plainAuthBytes = Encoding.ASCII.GetBytes("username:password");
                string base64AuthParam = Convert.ToBase64String(plainAuthBytes);

                Assert.Equal(base64AuthParam, client.Object.DefaultRequestHeaders.Authorization.Parameter);
                Assert.Equal("Basic", client.Object.DefaultRequestHeaders.Authorization.Scheme);

                return Task.FromResult(new HttpResponseMessage(responseStatusCode));
            });

            Func<Uri, StreamContent, Task<HttpResponseMessage>> runPostAsync = (uri, streamContent) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            };

            ZipDeployTask zipDeployer = new ZipDeployTask();

            bool result = await zipDeployer.ZipDeployAsync(TestZippedPublishContentsPath, "username", "password", "siteName", client.Object);

            client.Verify(c => c.PostAsync(
                It.Is<Uri>(uri => string.Equals(uri.AbsoluteUri, "https://sitename.scm.azurewebsites.net/api/zipdeploy", StringComparison.Ordinal)), 
                It.Is<StreamContent>(streamContent => IsStreamContentEqualToFileContent(streamContent, TestZippedPublishContentsPath))),
                Times.Once);
            Assert.Equal(expectedResult, result);
        }

        private bool IsStreamContentEqualToFileContent(StreamContent streamContent, string filePath)
        {
            byte[] expectedZipByteArr = File.ReadAllBytes(filePath);
            Task<byte[]> t = streamContent.ReadAsByteArrayAsync();
            t.Wait();
            return expectedZipByteArr.SequenceEqual(t.Result);
        }
    }
}
