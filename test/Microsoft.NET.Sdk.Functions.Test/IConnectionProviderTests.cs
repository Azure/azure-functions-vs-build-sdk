using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Microsoft.NET.Sdk.Functions.Test
{
    [StorageAccount("bar")]
    public class FunctionsClass1
    {
        [FunctionName("QueueFunc")]
        [StorageAccount("foo")]
        public static void RunQueue([QueueTrigger("queue1")] string message)
        {

        }
    }

    [StorageAccount("bar")]
    public class FunctionsClass2
    {
        [FunctionName("QueueFunc")]
        [StorageAccount("foo")]
        public static void RunQueue([QueueTrigger("queue1")] string message)
        {

        }
    }

    [StorageAccount("bar")]
    public class FunctionsClass3
    {
        [FunctionName("QueueFunc")]
        [StorageAccount("foo")]
        public static void RunQueue([StorageAccount("foobar")][QueueTrigger("queue1")] string message)
        {

        }
    }

    [StorageAccount("bar")]
    public class FunctionsClass4
    {
        [FunctionName("QueueFunc")]
        [StorageAccount("foo")]
        public static void RunQueue([StorageAccount("foobar")][QueueTrigger("queue1", Connection = "foobarfoobar")] string message)
        {

        }
    }

    public class IConnectionProviderTests
    {

    }
}
