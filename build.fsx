#r "packages/FAKE/tools/FakeLib.dll"
#r "packages/WindowsAzure.Storage/lib/net45/Microsoft.WindowsAzure.Storage.dll"

open System
open System.IO
open System.Net
open System.Threading.Tasks

open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Queue
open Fake
open Fake.AssemblyInfoFile

type Result<'TSuccess,'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure

let inline awaitTask (task: Task) =
    task
    |> Async.AwaitTask
    |> Async.RunSynchronously

let MoveFileTo (source, destination) =
    if File.Exists destination then
        File.Delete destination
    File.Move (source, destination)

let env = Environment.GetEnvironmentVariable
let connectionString =
    "DefaultEndpointsProtocol=https;AccountName=" + (env "FILES_ACCOUNT_NAME") + ";AccountKey=" + (env "FILES_ACCOUNT_KEY")
let buildTaskOutputPath = "src\\Microsoft.NET.Sdk.Functions.MSBuild\\bin\\Release"
let generatorOutputPath = "src\\Microsoft.NET.Sdk.Functions.Generator\\bin\\Release"
let packOutputPath = "pack\\Microsoft.NET.Sdk.Functions\\bin\\Release"
let version = if isNull appVeyorBuildVersion then "1.0.0.3" else appVeyorBuildVersion

Target "Clean" (fun _ ->
    if Directory.Exists "tmpBuild" |> not then Directory.CreateDirectory "tmpBuild" |> ignore
    if Directory.Exists "deploy" |> not then Directory.CreateDirectory "deploy" |> ignore
    CleanDir "tmpBuild"
    CleanDir "deploy"
)

Target "Build" (fun _ ->
    DotNetCli.Build (fun p ->
        {p with
            Project = "src\\Microsoft.NET.Sdk.Functions.MSBuild"
            Configuration = "Release"})

    DotNetCli.Build (fun p ->
        {p with
            Project = "src\\Microsoft.NET.Sdk.Functions.Generator"
            Configuration = "Release"})

    DotNetCli.Build (fun p ->
        {p with
            Project = "pack\\Microsoft.NET.Sdk.Functions"
            Configuration = "Release"})
)

Target "UnitTest" (fun _ ->

    DotNetCli.Build (fun p ->
        {p with
            Project = "test\\Microsoft.NET.Sdk.Functions.Generator.Tests"
            Configuration = "Debug"})

    DotNetCli.Test (fun p ->
        {p with
            AdditionalArgs = [ "--no-build" ]
            Project = "test\\Microsoft.NET.Sdk.Functions.Generator.Tests"
            Configuration = "Debug"})

    DotNetCli.Build (fun p ->
        {p with
            Project = "test\\Microsoft.NET.Sdk.Functions.Generator.V1.Tests"
            Configuration = "Debug"})

    DotNetCli.Test (fun p ->
        {p with
            AdditionalArgs = ["--no-build"]
            Project = "test\\Microsoft.NET.Sdk.Functions.Generator.V1.Tests"
            Configuration = "Debug"})

    DotNetCli.Build (fun p ->
        {p with
            Project = "test\\Microsoft.NET.Sdk.Functions.MSBuild.Tests"
            Configuration = "Debug"})

    DotNetCli.Test (fun p ->
        {p with
            AdditionalArgs = ["--no-build"]
            Project = "test\\Microsoft.NET.Sdk.Functions.MSBuild.Tests"
            Configuration = "Debug"})
)

Target "GenerateZipToSign" (fun _ ->
    !! (packOutputPath @@ "net46\\Microsoft.NET.Sdk.Functions.dll")
    ++ (buildTaskOutputPath @@ "net46\\Microsoft.NET.Sdk.Functions.MSBuild.dll")
    ++ (generatorOutputPath @@ "net461\\Microsoft.NET.Sdk.Functions.Generator.exe")
    |> CreateZip "." (version + "net46.zip") "" 7 true

    !! (generatorOutputPath @@ "net461\\Newtonsoft.Json.dll")
    ++ (generatorOutputPath @@ "net461\\Mono.Cecil.dll")
    |> CreateZip "." (version + "net46thirdparty.zip") "" 7 true

    !! (packOutputPath @@ "netstandard2.0\\Microsoft.NET.Sdk.Functions.dll")
    ++ (buildTaskOutputPath @@ "netstandard1.5\\Microsoft.NET.Sdk.Functions.MSBuild.dll")
    ++ (generatorOutputPath @@ "netcoreapp2.1\\Microsoft.NET.Sdk.Functions.Generator.dll")
    |> CreateZip "." (version + "netstandard2.zip") "" 7 true

    !! (generatorOutputPath @@ "netcoreapp2.1\\Newtonsoft.Json.dll")
    ++ (generatorOutputPath @@ "netcoreapp2.1\\Mono.Cecil.dll")
    |> CreateZip "." (version + "netstandard2thidparty.zip") "" 7 true
)

let storageAccount = lazy CloudStorageAccount.Parse connectionString
let blobClient = lazy storageAccount.Value.CreateCloudBlobClient ()
let queueClient = lazy storageAccount.Value.CreateCloudQueueClient ()
    
let UploadZip fileName =
    let container = blobClient.Value.GetContainerReference "azure-functions-build-sdk"
    container.CreateIfNotExists () |> ignore
    let blobRef = container.GetBlockBlobReference fileName
    blobRef.UploadFromStream <| File.OpenRead fileName
    
let EnqueueMessage (msg: string) =
    let queue = queueClient.Value.GetQueueReference "signing-jobs"
    let message = CloudQueueMessage msg
    queue.AddMessage message

let rec DownloadFile fileName (startTime: DateTime) = async {
    let container = blobClient.Value.GetContainerReference "azure-functions-build-sdk-signed"
    container.CreateIfNotExists () |> ignore
    let blob = container.GetBlockBlobReference fileName
    if blob.Exists () then
        blob.DownloadToFile ("signed-" + fileName, FileMode.OpenOrCreate)
        return Success ("signed-" + fileName)
    elif startTime.AddMinutes 20.0 < DateTime.UtcNow then
        return Failure "Timeout"
    else
        do! Async.Sleep 5000
        return! DownloadFile fileName startTime
}

Target "UploadZipToSign" (fun _ ->
    UploadZip (version + "net46.zip")
    UploadZip (version + "net46thirdparty.zip")
    UploadZip (version + "netstandard2.zip")
    UploadZip (version + "netstandard2thidparty.zip")
)

Target  "EnqueueSignMessage" (fun _ ->
    EnqueueMessage ("Sign;azure-functions-build-sdk;" + (version + "net46.zip"))
    EnqueueMessage ("Sign3rdParty;azure-functions-build-sdk;" + (version + "net46thirdparty.zip"))
    EnqueueMessage ("Sign;azure-functions-build-sdk;" + (version + "netstandard2.zip"))
    EnqueueMessage ("Sign3rdParty;azure-functions-build-sdk;" + (version + "netstandard2thidparty.zip"))
)

Target "WaitForSigning" (fun _ ->
    let signed = DownloadFile (version + "net46.zip") DateTime.UtcNow |> Async.RunSynchronously
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.dll", packOutputPath @@ "net46\\Microsoft.NET.Sdk.Functions.dll")
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.MSBuild.dll", buildTaskOutputPath @@ "net46\\Microsoft.NET.Sdk.Functions.MSBuild.dll")
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.Generator.exe", generatorOutputPath @@ "net461\\Microsoft.NET.Sdk.Functions.Generator.exe")
    | Failure e -> targetError e null |> ignore

    CleanDir "tmpBuild"

    let signed = DownloadFile (version + "netstandard2.zip") DateTime.UtcNow |> Async.RunSynchronously
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.dll", packOutputPath @@ "netstandard2.0\\Microsoft.NET.Sdk.Functions.dll")
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.MSBuild.dll", buildTaskOutputPath @@ "netstandard1.5\\Microsoft.NET.Sdk.Functions.MSBuild.dll")
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.Generator.dll", generatorOutputPath @@ "netcoreapp2.1\\Microsoft.NET.Sdk.Functions.Generator.dll")
    | Failure e -> targetError e null |> ignore

    CleanDir "tmpBuild"

    let signed = DownloadFile (version + "net46thirdparty.zip") DateTime.UtcNow |> Async.RunSynchronously
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        MoveFileTo ("tmpBuild" @@ "Newtonsoft.Json.dll", generatorOutputPath @@ "net461\\Newtonsoft.Json.dll")
        MoveFileTo ("tmpBuild" @@ "Mono.Cecil.dll", generatorOutputPath @@ "net461\\Mono.Cecil.dll")
    | Failure e -> targetError e null |> ignore

    CleanDir "tmpBuild"

    let signed = DownloadFile (version + "netstandard2thidparty.zip") DateTime.UtcNow |> Async.RunSynchronously
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        MoveFileTo ("tmpBuild" @@ "Newtonsoft.Json.dll", generatorOutputPath @@ "netcoreapp2.1\\Newtonsoft.Json.dll")
        MoveFileTo ("tmpBuild" @@ "Mono.Cecil.dll", generatorOutputPath @@ "netcoreapp2.1\\Mono.Cecil.dll")
    | Failure e -> targetError e null |> ignore
)

Target "Pack" (fun _ ->
    DotNetCli.Pack (fun p ->
        {p with
            Project = "pack\\Microsoft.NET.Sdk.Functions"
            Configuration = "Release"
            AdditionalArgs = [ "--no-build" ]})
)

Target "SignNupkg" (fun _ ->
    !! (packOutputPath @@ "/**/Microsoft.NET.Sdk.Functions.*.nupkg")
    |> CreateZip "." (version + "nupkg.zip") "" 7 true

    UploadZip (version + "nupkg.zip")
    EnqueueMessage ("SignNuget;azure-functions-build-sdk;" + (version + "nupkg.zip"))

    let signed = DownloadFile (version + "nupkg.zip") DateTime.UtcNow |> Async.RunSynchronously
    let signedOutputPath = packOutputPath @@ "signed"
    if Directory.Exists signedOutputPath |> not then Directory.CreateDirectory signedOutputPath |> ignore
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        let nupkgs = !! ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.*.nupkg")
        for nupkg in nupkgs do            
            MoveFileTo ("tmpBuild" @@ Path.GetFileName(nupkg), signedOutputPath @@ Path.GetFileName(nupkg))
    | Failure e -> targetError e null |> ignore
)

Target "Publish" (fun _ ->
    !! (packOutputPath @@ "signed\\Microsoft.NET.Sdk.Functions.*.nupkg")
    |> Seq.iter (MoveFile "deploy")
)

Dependencies
"Clean"
    ==> "Build"
    ==> "UnitTest"
    ==> "GenerateZipToSign"
    ==> "UploadZipToSign"
    ==> "EnqueueSignMessage"
    ==> "WaitForSigning"
    ==> "Pack"
    ==> "SignNupkg"
    ==> "Publish"

RunTargetOrDefault "Publish"
