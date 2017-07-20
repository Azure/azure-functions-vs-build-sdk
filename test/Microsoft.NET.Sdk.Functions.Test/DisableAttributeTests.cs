using System;
using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class DisableAttributeTests
    {
        public class FunctionsClass1
        {
            public static void Run1([Disable] [QueueTrigger("")] string message) { }

            public static void Run2([QueueTrigger("")] string message) { }

            [Disable]
            public static void Run3([QueueTrigger("")] string message) { }
        }

        [Disable]
        public class FunctionsClass2
        {
            public static void Run([QueueTrigger("")] string message) { }
        }

        public class FunctionsClass3
        {
            public static void Run([QueueTrigger("")] string message) { }
        }

        [Theory]
        [InlineData(typeof(FunctionsClass1), "Run1", true)]
        [InlineData(typeof(FunctionsClass1), "Run2", false)]
        [InlineData(typeof(FunctionsClass1), "Run3", true)]
        [InlineData(typeof(FunctionsClass2), "Run", true)]
        [InlineData(typeof(FunctionsClass3), "Run", false)]
        public void MethodsWithDisabledParametersShouldBeDisabled(Type type, string methodName, bool expectedIsDisabled)
        {
            var method = type.GetMethod(methodName);
            var funcJson = method.ToFunctionJson(string.Empty);
            funcJson.Disabled.Should().Be(expectedIsDisabled);
        }
    }
}
