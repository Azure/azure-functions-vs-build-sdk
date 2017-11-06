using System;
using System.Collections.Generic;
using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class GeneralWebJobsAttributesTests
    {
        public static IEnumerable<object[]> GetAttributes()
        {
            yield return new object[] { new QueueTriggerAttribute(string.Empty) };
            yield return new object[] { new BlobTriggerAttribute(string.Empty) };
            yield return new object[] { new EventHubTriggerAttribute(string.Empty) };
            yield return new object[] { new ServiceBusTriggerAttribute(string.Empty) };
        }

        [Theory]
        [MemberData(nameof(GetAttributes))]
        public void IsWebJobsAttribute(Attribute attribute)
        {
            attribute.IsWebJobsAttribute().Should().BeTrue(because: $"{attribute.GetType().FullName} is a WebJob's attribute");
        }
    }
}
