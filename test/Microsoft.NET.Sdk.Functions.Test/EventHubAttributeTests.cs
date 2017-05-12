using FluentAssertions;
using FluentAssertions.Json;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs.ServiceBus;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class EventHubAttributeTests
    {
        [Fact]
        public void EventHubShouldHaveCorrectPath()
        {
            var attribute = new EventHubAttribute("queue1");

            var jObject = attribute.ToJObject();

            jObject.Should().HaveElement("path");
            jObject["path"].Should().Be("queue1");
        }
    }
}
