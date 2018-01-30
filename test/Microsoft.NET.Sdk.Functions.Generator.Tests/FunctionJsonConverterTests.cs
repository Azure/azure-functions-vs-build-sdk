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

namespace Microsoft.NET.Sdk.Functions.Test
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
        [InlineData("MyHttpTrigger", "httpTrigger", "request")]
        [InlineData("MyBlobTrigger", "blobTrigger", "blobContent")]
        [InlineData("MyQueueTrigger", "queueTrigger", "queue")]
        [InlineData("MyEventHubTrigger", "eventHubTrigger", "message")]
        [InlineData("MyTimerTrigger", "timerTrigger", "timer")]
        [InlineData("MyServiceBusTrigger", "serviceBusTrigger", "message")]
        [InlineData("MyManualTrigger", "manualTrigger", "input")]
        [InlineData("MyManualTriggerWithoutParameters", "manualTrigger", null)]
        public void FunctionMethodsAreExported(string functionName, string type, string parameterName)
        {
            var logger = new RecorderLogger();
            var converter = new FunctionJsonConverter(logger, ".", ".");
            var functions = converter.GenerateFunctions(new [] {typeof(FunctionsClass)});
            var schema = functions.Single(e => Path.GetFileName(e.Value.outputFile.DirectoryName) == functionName).Value.schema;
            var binding = schema.Bindings.Single(); 
            binding.Value<string>("type").Should().Be(type);
            binding.Value<string>("name").Should().Be(parameterName);
            logger.Errors.Should().BeEmpty();
            logger.Warnings.Should().BeEmpty();
        }
        
        public class InvalidFunctionBecauseOfMissingTrigger
        {
            [FunctionName("MyServiceBusTrigger")]
            public static void Run(string message) { }
        }

        public class InvalidFunctionBecauseOfMissingFunctionName
        {
            public static void Run([ServiceBusTrigger("queue")] string message) { }
        }

        public class InvalidFunctionBecauseOfBothNoAutomaticTriggerAndServiceBusTrigger
        {
            [FunctionName("MyServiceBusTrigger"), NoAutomaticTrigger]
            public static void Run([ServiceBusTrigger("queue")] string message) { }
        }
        
        [Theory]
        [InlineData(typeof(InvalidFunctionBecauseOfMissingTrigger), "Method Microsoft.NET.Sdk.Functions.Test.FunctionJsonConverterTests+InvalidFunctionBecauseOfMissingTrigger.Run is missing a trigger attribute. Both a trigger attribute and FunctionName attribute are required for an Azure function definition.")]
        // [InlineData(typeof(InvalidFunctionBecauseOfMissingFunctionName), "Method Microsoft.NET.Sdk.Functions.Test.FunctionJsonConverterTests+InvalidFunctionBecauseOfMissingFunctionName.Run is missing the 'FunctionName' attribute. Both a trigger attribute and 'FunctionName' are required for an Azure function definition.")]
        [InlineData(typeof(InvalidFunctionBecauseOfBothNoAutomaticTriggerAndServiceBusTrigger), "Method Microsoft.NET.Sdk.Functions.Test.FunctionJsonConverterTests+InvalidFunctionBecauseOfBothNoAutomaticTriggerAndServiceBusTrigger.Run has both a 'NoAutomaticTrigger' attribute and a trigger attribute. Both can't be used together for an Azure function definition.")]
        public void InvalidFunctionMethodProducesWarning(Type type, string warningMessage)
        {
            var logger = new RecorderLogger();
            var converter = new FunctionJsonConverter(logger, ".", ".");
            var functions = converter.GenerateFunctions(new [] {type});
            functions.Should().BeEmpty();
            logger.Errors.Should().BeEmpty();
            logger.Warnings.Should().ContainSingle();
            logger.Warnings.Single().Should().Be(warningMessage);
        }
    }
}
