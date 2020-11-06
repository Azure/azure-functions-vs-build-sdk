using DirectRefEMG;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(DirectRefStartup))]

namespace DirectRefEMG
{
    public class DirectRefStartup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
        }
    }
}