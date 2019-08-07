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
        public static void EventHubTriggerAttribute_ShouldHaveV1vsV2Differences()
        {
            var attribute = new EventHubTriggerAttribute("eventHub");

            var jObject = attribute.ToJObject();

#if NET46
            jObject.Should().HaveElement("path");
            jObject["path"].Should().Be("eventHub");
#else
            jObject.Should().HaveElement("type");
            jObject["type"].Should().Be("eventHubTrigger");

            jObject.Should().HaveElement("path");
            jObject["path"].Should().Be("eventHub");
#endif
        }
    }
}
