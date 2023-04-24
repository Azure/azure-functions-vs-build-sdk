using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.MSBuild.Properties;
using Microsoft.NET.Sdk.Functions.MSBuild.Tasks;

namespace Microsoft.NET.Sdk.Functions.Tasks
{
    public class ZipDeployTask : Task
    {
        private const string UserAgentName = "functions-core-tools";

        [Required]
        public string ZipToPublishPath { get; set; }

        [Required]
        public string DeploymentUsername { get; set; }

        [Required]
        public string DeploymentPassword { get; set; }

        [Required]
        public string UserAgentVersion { get; set; }

        public string PublishUrl { get; set; }


        /// <summary>
        /// Our fallback if PublishUrl is not given, which is the case for ZIP Deploy profiles created prior to 15.8 Preview 4.
        /// Using this will fail if the site is a slot.
        /// </summary>
        public string SiteName { get; set; }

        public override bool Execute()
        { 
            using(DefaultHttpClient client = new DefaultHttpClient())
            {
                System.Threading.Tasks.Task<bool> t = ZipDeployAsync(ZipToPublishPath, DeploymentUsername, DeploymentPassword, PublishUrl, SiteName, UserAgentVersion, client, true);
                t.Wait();
                return t.Result;
            }
        }

        internal async System.Threading.Tasks.Task<bool> ZipDeployAsync(string zipToPublishPath, string userName, string password, string publishUrl, string siteName, string userAgentVersion, IHttpClient client, bool logMessages)
        {
            if (!File.Exists(zipToPublishPath) || client == null)
            {
                return false;
            }

            string zipDeployPublishUrl;
            if (!string.IsNullOrEmpty(publishUrl))
            {
                if (!publishUrl.EndsWith("/"))
                {
                    publishUrl += "/";
                }

                zipDeployPublishUrl = publishUrl + "api/zipdeploy";
            }
            else if(!string.IsNullOrEmpty(siteName))
            {
                zipDeployPublishUrl = $"https://{siteName}.scm.azurewebsites.net/api/zipdeploy";
            }
            else
            {
                if(logMessages)
                {
                    Log.LogError(Resources.NeitherSiteNameNorPublishUrlGivenError);
                }

                return false;
            }

            if (logMessages)
            {
                Log.LogMessage(MessageImportance.High, String.Format(Resources.PublishingZipViaZipDeploy, zipToPublishPath, zipDeployPublishUrl));
            }

            // use the async version of the api
            Uri uri = new Uri($"{zipDeployPublishUrl}?isAsync=true", UriKind.Absolute);
            string userAgent = $"{UserAgentName}/{userAgentVersion}";
            FileStream stream = File.OpenRead(zipToPublishPath);
            IHttpResponse response = await client.PostRequestAsync(uri, userName, password, "application/zip", userAgent, Encoding.UTF8, stream);
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                if (logMessages)
                {
                    Log.LogError(String.Format(Resources.ZipDeployFailureErrorMessage, zipDeployPublishUrl, response.StatusCode));
                }

                return false;
            }
            else
            {
                if (logMessages)
                {
                    Log.LogMessage(Resources.ZipFileUploaded);
                }

                string deploymentUrl = response.GetHeader("Location").FirstOrDefault();
                if (!string.IsNullOrEmpty(deploymentUrl))
                {
                    ZipDeploymentStatus deploymentStatus = new ZipDeploymentStatus(client, userAgent, Log, logMessages);
                    DeployStatus status = await deploymentStatus.PollDeploymentStatusAsync(deploymentUrl, userName, password);
                    if (status == DeployStatus.Success)
                    {
                        Log.LogMessage(MessageImportance.High, Resources.ZipDeploymentSucceeded);
                        return true;
                    }
                    else if (status == DeployStatus.Failed || status == DeployStatus.Unknown)
                    {
                        Log.LogError(String.Format(Resources.ZipDeploymentFailed, zipDeployPublishUrl, status));
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

