using FluentAssertions;
using FluentAssertions.Json;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Linq;
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
            jObject["authLevel"].Should().Be("function");
        }

        [Fact]
        public void HttpTriggerAttributeWithWebHookTypeShouldntHaveAnAuthLevel()
        {
            var attribute = new HttpTriggerAttribute()
            {
                WebHookType = "something"
            };

            var jObject = attribute.ToJObject();

            jObject["authLevel"].Should().BeNull();
        }
    }
}
