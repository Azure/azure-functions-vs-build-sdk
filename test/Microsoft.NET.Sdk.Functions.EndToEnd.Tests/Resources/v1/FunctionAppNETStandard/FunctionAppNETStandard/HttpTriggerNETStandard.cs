using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace FunctionAppNETStandard
{
    public static class HttpTriggerNETStandard
    {
        [FunctionName("HttpTriggerNETStandard")]
        public static IActionResult Run([HttpTrigger]HttpRequest req, ILogger log)
        {
            //log.Info("C# HTTP trigger function processed a request.");

            if (req.Query.TryGetValue("name", out StringValues value))
            {
                return new OkObjectResult($"Hello, {value.First()}");
            }

            return new BadRequestObjectResult("Please pass a name on the query string");
        }
    }
}