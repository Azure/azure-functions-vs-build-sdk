using System;
using System.Linq;
using System.Runtime.Loader;
using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    [StorageAccount("bar")]
    public class FunctionsClass1
    {
        public static void Run([QueueTrigger("queue1")] string message)
        {

        }
    }

    [StorageAccount("bar")]
    public class FunctionsClass2
    {
        [StorageAccount("foo")]
        public static void Run([QueueTrigger("queue1")] string message)
        {

        }
    }

    [StorageAccount("bar")]
    public class FunctionsClass3
    {
        [StorageAccount("foo")]
        public static void Run([StorageAccount("foobar")][QueueTrigger("queue1")] string message)
        {

        }
    }

    [StorageAccount("bar")]
    public class FunctionsClass4
    {
        [StorageAccount("foo")]
        public static void Run([StorageAccount("foobar")][QueueTrigger("queue1", Connection = "foobarfoobar")] string message)
        {

        }
    }

    public class IConnectionProviderTests
    {
        public IConnectionProviderTests()
        {
            AssemblyLoadContext.Default.EnterContextualReflection();
        }

        [Theory]
        [InlineData(typeof(FunctionsClass1), "bar")]
        [InlineData(typeof(FunctionsClass2), "foo")]
        [InlineData(typeof(FunctionsClass3), "foobar")]
        [InlineData(typeof(FunctionsClass4), "foobarfoobar")]
        public void TestIConnectionProviderHierarchicalLogic(Type type, string expected)
        {
            var method = TestUtility.GetMethodDefinition(type, "Run");

            var parameterInfo = method.Parameters.First();
            var attribute = parameterInfo.GetCustomAttribute(typeof(QueueTriggerAttribute));

            var resolvedAttribute = TypeUtility.GetResolvedAttribute(parameterInfo, attribute);

            resolvedAttribute.GetValue<string>("Connection").Should().Be(expected);
        }
    }
}
