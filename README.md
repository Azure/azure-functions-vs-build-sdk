![Azure Functions Logo](https://raw.githubusercontent.com/Azure/azure-functions-cli/main/eng/res/functions.png)

|Branch|Status|
|---|---|
|main|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/Azure.azure-functions-vs-build-sdk?branchName=main)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=52&branchName=main)|
|v4.x|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/Azure.azure-functions-vs-build-sdk?branchName=v4.x)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=52&branchName=v4.x)|
|release/4|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/Azure.azure-functions-vs-build-sdk?branchName=release%2F4)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=52&branchName=release%2F4)|
|v3.x|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/Azure.azure-functions-vs-build-sdk?branchName=v3.x)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=52&branchName=v3.x)|

# FAQ:

##### Q: I need a different `Newtonsoft.Json` version. What do I do?
Add the version you need to your `csproj`. For example to use `11.0.2` add this to your `csproj`

```xml
<PackageReference Include="Newtonsoft.Json" Version="11.0.2" />
```

##### Q: Why is `Newtonsoft.Json` locked in the first place?
The version of `Newtonsoft.Json` is locked to match the version used by the functions runtime. The reason is if you have a function like this

```cs
[FunctionName("hello")]
public static async Task ProcessQueue([QueueTrigger] JObject jObject)
{
    // do stuff;
}
```

That `jObject` instance will be fulfilled by the runtime version of `JObject`. If there is a version mismatch, the runtime will not be able to give you the version of `JObject` you are using from your custom `Newtonsoft.Json` version.

If you don't require `Newtonsoft.Json` objects to be fulfilled by the runtime, then you can specify the version you like to use in your own functions in your `csproj`

#### Q: What version of the runtime is this package version?
None. This is a build task for building .NET function projects. This doesn't bring in a runtime version, only attributes versions. The runtime version is decided by Azure, or your version of the [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)



# Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
