using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Loader;
using Azure.Messaging.EventHubs;
using FluentAssertions;
using MakeFunctionJson;
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

            [FunctionName("HttpTriggerQueueReturn")]
            [return: Queue("myqueue-items-a", Connection = "MyStorageConnStrA")]
            public static string HttpTriggerQueueReturn([HttpTrigger] HttpRequestMessage request) => "foo";

            [FunctionName("HttpTriggerQueueOutParam")]
            public static void HttpTriggerQueueOutParam([HttpTrigger] HttpRequestMessage request,
                [Queue("myqueue-items-b", Connection = "MyStorageConnStrB")] out string msg)
            {
                msg = "foo";
            }

            [FunctionName("HttpTriggerMultipleOutputs")]
            public static void HttpTriggerMultipleOutputs([HttpTrigger] HttpRequestMessage request,
                [Blob("binding-metric-test/sample-text.txt", Connection = "MyStorageConnStrC")] out string myBlob,
                [Queue("myqueue-items-c", Connection = "MyStorageConnStrC")] IAsyncCollector<string> qCollector)
            {
                myBlob = "foo-blob";
                qCollector.AddAsync("foo-queue");
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
            
            public Dictionary<string, string>[] Bindings { set; get; }
        }

        public class BindingTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyHttpTrigger",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "httpTrigger" },
                               {"name" , "request"},
                               {"authLevel" , "function"}
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="HttpTriggerQueueReturn",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "httpTrigger" },
                               {"name" , "request"},
                               {"authLevel" , "function"}
                            },
                            new Dictionary<string, string>
                            {
                               {"type", "queue" },
                               {"name" , "$return"},
                               {"connection" , "MyStorageConnStrA"},
                               {"queueName","myqueue-items-a" }
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="HttpTriggerQueueOutParam",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "httpTrigger" },
                               {"name" , "request"},
                               {"authLevel" , "function"}
                            },
                            new Dictionary<string, string>
                            {
                               {"type", "queue" },
                               {"name" , "msg"},
                               {"connection" , "MyStorageConnStrB"},
                               {"queueName","myqueue-items-b" }
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="HttpTriggerMultipleOutputs",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "httpTrigger" },
                               {"name" , "request"},
                               {"authLevel" , "function"}
                            },
                            new Dictionary<string, string>
                            {
                               {"type", "queue" },
                               {"name" , "qCollector"},
                               {"connection" , "MyStorageConnStrC"},
                               {"queueName","myqueue-items-c" }
                            },
                            new Dictionary<string, string>
                            {
                               {"type", "blob" },
                               {"name" , "myBlob"},
                               {"blobPath", "binding-metric-test/sample-text.txt" },
                               {"connection" , "MyStorageConnStrC"}
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyBlobTrigger",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "blobTrigger" },
                               {"name" , "blobContent"},
                               {"path" , "blob.txt"}
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyEventHubTrigger",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "eventHubTrigger" },
                               {"name" , "message"},
                               {"eventHubName" , "hub"}
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyTimerTrigger",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "timerTrigger" },
                               {"name" , "timer"},
                               {"schedule" , "00:30:00"},
                               {"useMonitor" , "True"},
                               {"runOnStartup" , "False"}
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyServiceBusTrigger",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "serviceBusTrigger" },
                               {"name" , "message"},
                               {"queueName" , "queue"},
                               {"isSessionsEnabled" , "False"}
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyManualTrigger",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "manualTrigger" },
                               {"name" , "input"}
                            }
                        }
                    }
                };

                yield return new object[] {
                    new BindingAssertionItem
                    {
                        FunctionName="MyManualTriggerWithoutParameters",
                        Bindings = new Dictionary<string, string>[]
                        {
                            new Dictionary<string, string>
                            {
                               {"type", "manualTrigger" }
                            }
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
            var schemaActual = functions.Single(e => Path.GetFileName(e.Value.outputFile.DirectoryName) == item.FunctionName).Value.schema;

            foreach (var expectedBindingItem in item.Bindings)
            {
                var expectedBindingType = expectedBindingItem.FirstOrDefault(a => a.Key == "type");

                // query binding entry from actual using the type.
                var matchingBindingFromActual = schemaActual.Bindings
                                                            .First(a => a.Properties().Any(g => g.Name == "type"
                                                                                             && g.Value.ToString()== expectedBindingType.Value));

                // compare all props of binding entry from expected entry with actual.
                foreach (var prop in expectedBindingItem)
                {
                    // make sure the prop exist in the binding.
                    matchingBindingFromActual.ContainsKey(prop.Key).Should().BeTrue();

                    // Verify the prop values matches between expected and actual.
                    expectedBindingItem[prop.Key].Should().Be(matchingBindingFromActual[prop.Key].ToString());
                }
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
