using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using V2;

[assembly: FunctionsStartup(typeof(Startup))]

namespace V2
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
        }
    }
}
