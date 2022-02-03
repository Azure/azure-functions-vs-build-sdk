using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Loader;
using FluentAssertions;
using MakeFunctionJson;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Queue;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class FunctionJsonConverterTests
    {
        public FunctionJsonConverterTests()
        {
            AssemblyLoadContext.Default.EnterContextualReflection();
        }

        public class FunctionsClass
        {
            [FunctionName("MyHttpTrigger")]
            public static void Run1([HttpTrigger] HttpRequestMessage request) { }

            [FunctionName("HttpTriggerWriteToQueue1")]
            [return: Queue("myqueue-items-a", Connection = "MyStorageConnStra")]
            public static string HttpTriggerWriteToQueue1([HttpTrigger] HttpRequestMessage request) => "foo";

            [FunctionName("HttpTriggerWriteToQueue2")]
            public static void HttpTriggerWriteToQueue2([HttpTrigger] HttpRequestMessage request,
                [Queue("myqueue-items-b", Connection = "MyStorageConnStrb")] out string msg)
            {
                msg = "foo";
            }

            [FunctionName("HttpTriggerWriteToQueue3")]
            public static void HttpTriggerWriteToQueue3([HttpTrigger] HttpRequestMessage request,
            [Queue("myqueue-items-c", Connection = "MyStorageConnStrc")] IAsyncCollector<string> collector)
            {
                collector.AddAsync("foo");
            }

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

        public class BindingAssertionItem
        {
            public string FunctionName { set; get; }

            // key is binding type, value is binding parameter name.
            public Dictionary<string, string> Bindings { set; get; }
        }

        public class BindingTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyHttpTrigger",
                        Bindings= new Dictionary<string, string>
                        {
                            {"httpTrigger", "request"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="HttpTriggerWriteToQueue1",
                        Bindings= new Dictionary<string, string>
                        {
                           {"httpTrigger", "request"},
                           {"queue", "$return"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="HttpTriggerWriteToQueue2",
                        Bindings= new Dictionary<string, string>
                        {
                           {"httpTrigger", "request"},
                           {"queue", "msg"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="HttpTriggerWriteToQueue3",
                        Bindings= new Dictionary<string, string>
                        {
                           {"httpTrigger", "request"},
                           {"queue", "collector"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyBlobTrigger",
                        Bindings= new Dictionary<string, string>
                        {
                            {"blobTrigger", "blobContent"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyEventHubTrigger",
                        Bindings= new Dictionary<string, string>
                        {
                            {"eventHubTrigger", "message"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyTimerTrigger",
                        Bindings= new Dictionary<string, string>
                        {
                            {"timerTrigger", "timer"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyServiceBusTrigger",
                        Bindings= new Dictionary<string, string>
                        {
                            {"serviceBusTrigger", "message"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyManualTrigger",
                        Bindings= new Dictionary<string, string>
                        {
                            {"manualTrigger", "input"}
                        }
                    }
                };
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyManualTriggerWithoutParameters",
                        Bindings= new Dictionary<string, string>
                        {
                            {"manualTrigger", null}
                        }
                    }
                };
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(BindingTestData))]
        public void FunctionMethodsAreExported(BindingAssertionItem item)
        {
            var logger = new RecorderLogger();
            var converter = new FunctionJsonConverter(logger, ".", ".", functionsInDependencies: false);
            var functions = converter.GenerateFunctions(new[] { TestUtility.GetTypeDefinition(typeof(FunctionsClass)) }).ToArray();
            var schema = functions.Single(e => Path.GetFileName(e.Value.outputFile.DirectoryName) == item.FunctionName).Value.schema;

            schema.Bindings.Count().Should().Be(item.Bindings.Count);

            foreach (var binding in schema.Bindings)
            {
                var type=binding.Value<string>("type");
                var name=binding.Value<string>("name");

                name.Should().Be(item.Bindings[type]);
            }

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
            var converter = new FunctionJsonConverter(logger, ".", ".", functionsInDependencies: false);
            var functions = converter.GenerateFunctions(new[] { TestUtility.GetTypeDefinition(type) });
            functions.Should().BeEmpty();
            logger.Errors.Should().BeEmpty();
            logger.Warnings.Should().ContainSingle();
            logger.Warnings.Single().Should().Be(warningMessage);
        }
    }
}
