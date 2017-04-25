using FluentAssertions;
using FluentAssertions.Json;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class ServiceBusTriggerTests
    {
        [Fact]
        // https://github.com/Azure/azure-functions-vs-build-sdk/issues/1
        public void ServiceBusTriggerShouldHaveStringEnumForAccessRights()
        {
            var attribute = new ServiceBusTriggerAttribute("queue1", AccessRights.Manage);

            var jObject = attribute.ToJObject();

            jObject.Should().HaveElement("access");
            jObject["access"].Should().Be("manage");
        }
    }
}
