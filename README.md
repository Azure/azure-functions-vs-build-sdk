![Azure Functions Logo](https://raw.githubusercontent.com/Azure/azure-functions-cli/master/src/Azure.Functions.Cli/npm/assets/azure-functions-logo-color-raster.png)

|Branch|Status|
|---|---|
|master|[![Build status](https://ci.appveyor.com/api/projects/status/54b2dh9ge9f8g3mg/branch/master?svg=true)](https://ci.appveyor.com/project/appsvc/azure-functions-vs-build-sdk/branch/master)|

# FAQ:

##### Q: I need a different `Newtonsoft.Json` version?
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
    jObject.DoStuff();
}
```

That `jObject` instance will be fulfilled by the runtime version of `JObject`. If `DoStuff()` doesn't exist in the pinned version, you'll get a `MethodNotFoundException`. The solution is to use the `Newtonsoft.Json` version available to you from the runtime. 

If you don't require `Newtonsoft.Json` objects to be fulfilled by the runtime, then you can specify the version you like to use in your own functions in your `csproj`

#### Q: What version of the runtime is this package version?
None. This is a build task for building .NET function projects. This doesn't bring in a runtime version, only attributes versions. The runtime version is decided by Azure, or your version of the [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)



# Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
