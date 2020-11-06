using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using V3;

[assembly: FunctionsStartup(typeof(Startup))]

namespace V3
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
        }
    }
}
