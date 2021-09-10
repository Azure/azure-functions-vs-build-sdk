using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using V4;

[assembly: FunctionsStartup(typeof(Startup))]

namespace V4
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
        }
    }
}
