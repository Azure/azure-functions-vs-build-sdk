using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using SharedStartup;

[assembly: FunctionsStartup(typeof(Startup))]

namespace SharedStartup
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
        }
    }
}
