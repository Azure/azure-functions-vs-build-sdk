using System;
using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class GeneralWebJobsAttributesTests
    {
        [Fact]
        public void IsWebJobsAttribute()
        {
            var attributes = new Attribute[]
            {
                new QueueTriggerAttribute(string.Empty),
                new BlobTriggerAttribute(string.Empty),
                new EventHubTriggerAttribute(string.Empty),
                new ServiceBusTriggerAttribute(string.Empty)
            };

            foreach (var attribute in attributes)
            {
                attribute.IsWebJobsAttribute().Should().BeTrue(because: $"{attribute.GetType().FullName} is a WebJob's attribute");
            }

        }
    }
}
