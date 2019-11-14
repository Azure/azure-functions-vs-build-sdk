using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Mono.Cecil;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class GeneralWebJobsAttributesTests
    {
        private static void FakeFunction(
            [QueueTrigger("a")]
            [BlobTrigger("b")]
            [EventHubTrigger("c")]
            [ServiceBusTrigger("d")] string abcd)
        {
        }

        public static IEnumerable<object[]> GetAttributes()
        {
            return TestUtility.GetCustomAttributes(typeof(GeneralWebJobsAttributesTests), "FakeFunction", "abcd")
                .Select(p => new object[] { p });
        }

        [Theory]
        [MemberData(nameof(GetAttributes))]
        public void IsWebJobsAttribute(CustomAttribute attribute)
        {
            attribute.IsWebJobsAttribute().Should().BeTrue(because: $"{attribute.GetType().FullName} is a WebJob's attribute");
        }
    }
}
