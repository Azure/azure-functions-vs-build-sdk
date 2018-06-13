using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.MSBuild.Properties;

namespace Microsoft.NET.Sdk.Functions.Tasks
{
    public class ZipDeployTask : Task
    {
        [Required]
        public string ZipToPublishPath { get; set; }

        [Required]
        public string DeploymentUsername { get; set; }

        [Required]
        public string DeploymentPassword { get; set; }

        [Required]
        public string ScmSiteUrl { get; set; }

        public override bool Execute()
        { 
            using(DefaultHttpClient client = new DefaultHttpClient())
            {
                System.Threading.Tasks.Task<bool> t = ZipDeployAsync(ZipToPublishPath, DeploymentUsername, DeploymentPassword, ScmSiteUrl, client);
                t.Wait();
                return t.Result;
            }
        }

        private async System.Threading.Tasks.Task<bool> ZipDeployAsync(string zipToPublishPath, string userName, string password, string scmSiteUrl, IHttpClient client)
        {
            if (!File.Exists(ZipToPublishPath) || client == null)
            {
                return false;
            }

            if (!scmSiteUrl.EndsWith("/"))
            {
                scmSiteUrl += "/";
            }

            string zipDeployPublishUrl = scmSiteUrl  + "api/zipdeploy";

            Log.LogMessage(MessageImportance.High, String.Format(Resources.PublishingZipViaZipDeploy, zipToPublishPath, zipDeployPublishUrl));

            Uri uri = new Uri(zipDeployPublishUrl, UriKind.Absolute);
            FileStream stream = File.OpenRead(ZipToPublishPath);
            IHttpResponse response = await client.PostWithBasicAuthAsync(uri, userName, password, "application/zip", Encoding.UTF8, stream);
            if(response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                Log.LogError(String.Format(Resources.ZipDeployFailureErrorMessage, zipDeployPublishUrl, response.StatusCode));
                return false;
            }

            return true;
        }
    }
}

