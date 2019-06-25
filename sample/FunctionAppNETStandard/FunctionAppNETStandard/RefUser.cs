using System;
using FunctionsRefNETStandard;

namespace FunctionAppNETStandard
{
    // Uses references so compiler won't strip them out of the managed module.
    class RefUser
    {
        public Type FunctionsRefNetStandard { get => typeof(HttpTriggerRefNETStandard); }
    }
}
