using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test.V2
{
    public class FunctionJsonConverterTests
    {
        public class FunctionsClass
        {
            [FunctionName("MyHttpTrigger")]
            public static void Run1([HttpTrigger] HttpRequestMessage request) { }

            [FunctionName("MyBlobTrigger")]
            public static void Run2([BlobTrigger("blob.txt")] string blobContent) { }

            [FunctionName("MyQueueTrigger")]
            public static void Run3([QueueTrigger("queue")] CloudQueue queue) { }

            [FunctionName("MyEventHubTrigger")]
            public static void Run4([EventHubTrigger("hub")] EventData message) { }

            [FunctionName("MyTimerTrigger")]
            public static void Run5([TimerTrigger("00:30:00")] TimerInfo timer) { }

            [FunctionName("MyServiceBusTrigger")]
            public static void Run6([ServiceBusTrigger("queue")] string message) { }

            [FunctionName("MyManualTrigger"), NoAutomaticTrigger]
            public static void Run7(string input) { }

            [FunctionName("MyManualTriggerWithoutParameters"), NoAutomaticTrigger]
            public static void Run8() { }
        }

        [Theory]
        [InlineData("MyHttpTrigger", "httpTrigger", "request", "authLevel", "function")]
        [InlineData("MyBlobTrigger", "blobTrigger", "blobContent", "path", "blob.txt")]
        [InlineData("MyQueueTrigger", "queueTrigger", "queue", "queueName", "queue")]
        [InlineData("MyTimerTrigger", "timerTrigger", "timer", "schedule", "00:30:00")]
        [InlineData("MyServiceBusTrigger", "serviceBusTrigger", "message", "accessRights", "manage")]
        [InlineData("MyManualTrigger", "manualTrigger", "input", null, null)]
        [InlineData("MyManualTriggerWithoutParameters", "manualTrigger", null, null, null)]
        [InlineData("MyEventHubTrigger", "eventHubTrigger", "message", "path", "hub")]
        public void FunctionMethodsAreExported(string functionName, string type, string parameterName, string bindingName, string bindingValue)
        {
            var logger = new RecorderLogger();
            var converter = new FunctionJsonConverter(logger, ".", ".", functionsInDependencies: false);
            var functions = converter.GenerateFunctions(new[] { TestUtility.GetTypeDefinition(typeof(FunctionsClass)) });
            var schema = functions.Single(e => Path.GetFileName(e.Value.outputFile.DirectoryName) == functionName).Value.schema;
            var binding = schema.Bindings.Single();
            binding.Value<string>("type").Should().Be(type);
            binding.Value<string>("name").Should().Be(parameterName);
            if(bindingName != null)
            {
                binding.Value<string>(bindingName).Should().Be(bindingValue);
            }
            logger.Errors.Should().BeEmpty();
            logger.Warnings.Should().BeEmpty();
        }
    }
}
