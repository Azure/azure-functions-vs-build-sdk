using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace FunctionsRefNETStandard
{
    public class HttpTriggerRefNETStandard
    {
        [FunctionName("HttpTriggerRefNETStandard")]
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
