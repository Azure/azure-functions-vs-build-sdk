using FluentAssertions;
using FluentAssertions.Json;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class HttpTriggerTests
    {
        [Fact]
        //https://github.com/Azure/azure-functions-vs-build-sdk/issues/16
        public void HttpTriggerShouldHaveStringForAuthLevelEnum()
        {
            var attribute = new HttpTriggerAttribute(AuthorizationLevel.Function);

            var jObject = attribute.ToJObject();

            jObject.Should().HaveElement("authLevel");
            jObject["authLevel"].Should().HaveValue("function");
        }
    }
}
