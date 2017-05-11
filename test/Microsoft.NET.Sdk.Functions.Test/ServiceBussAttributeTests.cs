using FluentAssertions;
using FluentAssertions.Json;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class ServiceBussAttributeTests
    {
        [Fact]
        public void ServiceBusShouldHaveStringEnumForAccessRights()
        {
            var attribute = new ServiceBusAttribute("queue1", AccessRights.Manage);

            var jObject = attribute.ToJObject();

            jObject.Should().HaveElement("accessRights");
            jObject["accessRights"].Should().Be("manage");
        }

        [Fact]
        public void ServiceBusShouldHaveCorrectTopicName()
        {
            var attribute = new ServiceBusAttribute("queue1")
            {
                EntityType = EntityType.Topic
            };

            var jObject = attribute.ToJObject();

            jObject.Should().HaveElement("topicName");
            jObject["topicName"].Should().Be("queue1");
        }

        [Fact]
        public void ServiceBusShouldHaveCorrectQueueName()
        {
            var attribute = new ServiceBusAttribute("queue1")
            {
                EntityType = EntityType.Queue
            };

            var jObject = attribute.ToJObject();

            jObject.Should().HaveElement("queueName");
            jObject["queueName"].Should().Be("queue1");
        }
    }
}
