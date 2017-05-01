using FluentAssertions;
using FluentAssertions.Json;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class DocumentDbAttributeTests
    {
        [Fact]
        public void MethodsWithDisabledParametersShouldBeDisabled()
        {
            var attribute = new DocumentDBAttribute()
            {
                ConnectionStringSetting = "value"
            };

            var jObject = attribute.ToJObject();

            jObject.Should().HaveElement("connection");
            jObject["connection"].Should().Be("value");
        }
    }
}
