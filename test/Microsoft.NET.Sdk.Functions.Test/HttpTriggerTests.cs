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

        public class FunctionClass
        {
            public static void Run([HttpTrigger(WebHookType = "something")] string message) { }
        }

        [Theory]
        [InlineData(typeof(FunctionClass), "Run")]
        public void HttpTriggerAttributeWithWebHookTypeShouldntHaveAnAuthLevel(Type type, string methodName)
        {
            var method = type.GetMethod(methodName);
            var funcJson = method.ToFunctionJson(string.Empty);

            funcJson.Bindings.Should().HaveCount(2);
            funcJson.Bindings.First()["authLevel"].Should().BeNull();
        }
    }
}
