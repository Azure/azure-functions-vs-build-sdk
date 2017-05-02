using System;
using System.Reflection;
using Microsoft.Build.Utilities;

namespace MakeFunctionJson
{
#if NET46
    public class Proxy : MarshalByRefObject
    {
        private FunctionJsonConverter _converter;
        public Proxy(string assemblyPath, string outputPath, TaskLoggingHelper log)
        {
            this._converter = new FunctionJsonConverter(assemblyPath, outputPath, log);
        }

        public bool TryRun()
        {
            return this._converter.TryRun();
        }
    }
#endif

    public static class FunctionJsonConvert
    {
        public static bool TryConvert(string assemblyPath, string outputPath, TaskLoggingHelper log = null)
        {
#if NET46
            var appDomain = AppDomain.CreateDomain("loadDllDomain");
            var converter = (Proxy) appDomain.CreateInstanceFromAndUnwrap(typeof(Proxy).Assembly.Location,
                typeof(Proxy).FullName,
                false,
                BindingFlags.Default,
                null,
                new object[] { assemblyPath, outputPath, log },
                null, null);
            var result = converter.TryRun();
            AppDomain.Unload(appDomain);
            return result;
#else
            var converter = new FunctionJsonConverter(assemblyPath, outputPath, log);
            return converter.TryRun();
#endif
        }
    }
}
