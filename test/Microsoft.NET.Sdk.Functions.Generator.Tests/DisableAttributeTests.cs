using System;
using System.Reflection;
using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Mono.Cecil;
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

            [Disable("function-disabled-setting")]
            public static void Run4([QueueTrigger("")] string message) { }

            [Disable(typeof(FunctionsClass1))]
            public static void Run5([QueueTrigger("")] string message) { }

            public static bool IsDisabled(MethodInfo method) { return false; }
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

        public class FunctionsClass4
        {
            [Disable(typeof(FunctionsClass4))]
            public static void Run([QueueTrigger("")] string message) { }
        }

        [Theory]
        [InlineData(typeof(FunctionsClass1), "Run1", true)]
        [InlineData(typeof(FunctionsClass1), "Run2", false)]
        [InlineData(typeof(FunctionsClass1), "Run3", true)]
        [InlineData(typeof(FunctionsClass1), "Run4", "function-disabled-setting")]
        [InlineData(typeof(FunctionsClass2), "Run", true)]
        [InlineData(typeof(FunctionsClass3), "Run", false)]
        public void MethodsWithDisabledParametersShouldBeDisabled(Type type, string methodName, object expectedIsDisabled)
        {
            MethodDefinition methodDef = TestUtility.GetMethodDefinition(type, methodName);
            FunctionJsonSchema funcJson = methodDef.ToFunctionJson(string.Empty);
            funcJson.Disabled.Should().Be(expectedIsDisabled);
        }
    }
}
