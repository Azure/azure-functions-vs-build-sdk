using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class HasUnsupportedAttributesTests
    {
        public class FunctionsClass1
        {
            public static void Run1([Disable] [QueueTrigger("")] string message) { }

            public static void Run2([QueueTrigger("")] string message) { }

            [Disable]
            public static void Run3([QueueTrigger("")] string message) { }

            [Disable("function-disabled-setting")]
            public static void Run4([QueueTrigger("")] string message) { }

            [Disable("%function-disabled-setting%")]
            public static void Run5([QueueTrigger("")] string message) { }

            [Disable(typeof(FunctionsClass1))]
            public static void Run6([QueueTrigger("")] string message) { }

            public static bool IsDisabled(MethodInfo method) { return false; }
        }

        [Theory]
        [InlineData(typeof(FunctionsClass1), "Run1", false)]
        [InlineData(typeof(FunctionsClass1), "Run2", false)]
        [InlineData(typeof(FunctionsClass1), "Run3", false)]
        [InlineData(typeof(FunctionsClass1), "Run4", false)]
        [InlineData(typeof(FunctionsClass1), "Run5", true)]
        [InlineData(typeof(FunctionsClass1), "Run6", true)]
        public void HasUnsupportedAttributesWorksCorrectly(Type type, string methodName, bool expected)
        {
            var method = type.GetMethod(methodName);
            var hasUnsuportedAttribute = method.HasUnsuportedAttributes(out string _);
            hasUnsuportedAttribute.Should().Be(expected);
        }
    }
}
