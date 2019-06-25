using System;
using FunctionsRefSdkClassLib;

namespace FunctionAppNETFramework
{
    // Uses references so compiler won't strip them out of the managed module.
    class RefUser
    {
        public Type FunctionsRefSdkClassLibStandard { get => typeof(HttpTriggerRefSdkNETFramework); }
    }
}
