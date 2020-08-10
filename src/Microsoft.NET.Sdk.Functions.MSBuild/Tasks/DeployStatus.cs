﻿namespace Microsoft.NET.Sdk.Functions.MSBuild.Tasks
{
    public enum DeployStatus
    {
        Unknown = -1,
        Pending = 0,
        Building = 1,
        Deploying = 2,
        Failed = 3,
        Success = 4
    }
}
