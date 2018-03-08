#r "packages/FAKE/tools/FakeLib.dll"
#r "packages/WindowsAzure.Storage/lib/net40/Microsoft.WindowsAzure.Storage.dll"

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

Target "GenerateZipToSign" (fun _ ->
    !! (packOutputPath @@ "net46\\Microsoft.NET.Sdk.Functions.dll")
    ++ (buildTaskOutputPath @@ "net46\\Microsoft.NET.Sdk.Functions.MSBuild.dll")
    ++ (generatorOutputPath @@ "net461\\Microsoft.NET.Sdk.Functions.Generator.exe")
    |> CreateZip "." (version + "net46.zip") "" 7 true


    !! (generatorOutputPath @@ "net461\\Newtonsoft.Json.dll")
    |> CreateZip "." (version + "net46thirdparty.zip") "" 7 true

    !! (packOutputPath @@ "netstandard2.0\\Microsoft.NET.Sdk.Functions.dll")
    ++ (buildTaskOutputPath @@ "netstandard1.5\\Microsoft.NET.Sdk.Functions.MSBuild.dll")
    ++ (generatorOutputPath @@ "netcoreapp2.0\\Microsoft.NET.Sdk.Functions.Generator.dll")
    |> CreateZip "." (version + "netstandard2.zip") "" 7 true

    !! (generatorOutputPath @@ "netcoreapp2.0\\Newtonsoft.Json.dll")
    |> CreateZip "." (version + "netstandard2thidparty.zip") "" 7 true
)

let storageAccount = lazy CloudStorageAccount.Parse connectionString
let blobClient = lazy storageAccount.Value.CreateCloudBlobClient ()
let queueClient = lazy storageAccount.Value.CreateCloudQueueClient ()

Target "UploadZipToSign" (fun _ ->
    let container = blobClient.Value.GetContainerReference "azure-functions-cli"
    container.CreateIfNotExists () |> ignore
    let uploadZip fileName =
        let blobRef = container.GetBlockBlobReference fileName
        blobRef.UploadFromStream <| File.OpenRead fileName

    uploadZip (version + "net46.zip")
    uploadZip (version + "net46thirdparty.zip")
    uploadZip (version + "netstandard2.zip")
    uploadZip (version + "netstandard2thidparty.zip")
)

Target  "EnqueueSignMessage" (fun _ ->
    let queue = queueClient.Value.GetQueueReference "signing-jobs"
    let enqueueMessage (msg: string) =
        let message = CloudQueueMessage msg
        queue.AddMessage message
    enqueueMessage ("Sign;azure-functions-cli;" + (version + "net46.zip"))
    enqueueMessage ("Sign3rdParty;azure-functions-cli;" + (version + "net46thirdparty.zip"))
    enqueueMessage ("Sign;azure-functions-cli;" + (version + "netstandard2.zip"))
    enqueueMessage ("Sign3rdParty;azure-functions-cli;" + (version + "netstandard2thidparty.zip"))
)

Target "WaitForSigning" (fun _ ->
    let rec downloadFile fileName (startTime: DateTime) = async {
        let container = blobClient.Value.GetContainerReference "azure-functions-cli-signed"
        container.CreateIfNotExists () |> ignore
        let blob = container.GetBlockBlobReference fileName
        if blob.Exists () then
            blob.DownloadToFile ("signed-" + fileName, FileMode.OpenOrCreate)
            return Success ("signed-" + fileName)
        elif startTime.AddMinutes 10.0 < DateTime.UtcNow then
            return Failure "Timeout"
        else
            do! Async.Sleep 5000
            return! downloadFile fileName startTime
    }

    let signed = downloadFile (version + "net46.zip") DateTime.UtcNow |> Async.RunSynchronously
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.dll", packOutputPath @@ "net46\\Microsoft.NET.Sdk.Functions.dll")
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.MSBuild.dll", buildTaskOutputPath @@ "net46\\Microsoft.NET.Sdk.Functions.MSBuild.dll")
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.Generator.exe", generatorOutputPath @@ "net461\\Microsoft.NET.Sdk.Functions.Generator.exe")
    | Failure e -> targetError e null |> ignore

    CleanDir "tmpBuild"

    let signed = downloadFile (version + "netstandard2.zip") DateTime.UtcNow |> Async.RunSynchronously
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.dll", packOutputPath @@ "netstandard2.0\\Microsoft.NET.Sdk.Functions.dll")
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.MSBuild.dll", buildTaskOutputPath @@ "netstandard1.5\\Microsoft.NET.Sdk.Functions.MSBuild.dll")
        MoveFileTo ("tmpBuild" @@ "Microsoft.NET.Sdk.Functions.Generator.dll", generatorOutputPath @@ "netcoreapp2.0\\Microsoft.NET.Sdk.Functions.Generator.dll")
    | Failure e -> targetError e null |> ignore

    CleanDir "tmpBuild"

    let signed = downloadFile (version + "net46thirdparty.zip") DateTime.UtcNow |> Async.RunSynchronously
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        MoveFileTo ("tmpBuild" @@ "Newtonsoft.Json.dll", generatorOutputPath @@ "net461\\Newtonsoft.Json.dll")
    | Failure e -> targetError e null |> ignore

    CleanDir "tmpBuild"

    let signed = downloadFile (version + "netstandard2thidparty.zip") DateTime.UtcNow |> Async.RunSynchronously
    match signed with
    | Success file ->
        Unzip "tmpBuild" file
        MoveFileTo ("tmpBuild" @@ "Newtonsoft.Json.dll", generatorOutputPath @@ "netcoreapp2.0\\Newtonsoft.Json.dll")
    | Failure e -> targetError e null |> ignore

)

Target "Pack" (fun _ ->
    DotNetCli.Pack (fun p ->
        {p with
            Project = "pack\\Microsoft.NET.Sdk.Functions"
            Configuration = "Release"
            AdditionalArgs = [ "--no-build" ]})
)

Target "Publish" (fun _ ->
    !! (packOutputPath @@ "/**/*.nupkg")
    |> Seq.iter (MoveFile "deploy")
)

Dependencies
"Clean"
    ==> "Build"
    ==> "GenerateZipToSign"
    ==> "UploadZipToSign"
    ==> "EnqueueSignMessage"
    ==> "WaitForSigning"
    ==> "Pack"
    ==> "Publish"

RunTargetOrDefault "Publish"