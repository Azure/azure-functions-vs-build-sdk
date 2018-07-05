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
                System.Threading.Tasks.Task<bool> t = ZipDeployAsync(ZipToPublishPath, DeploymentUsername, DeploymentPassword, PublishUrl, SiteName, client, true);
                t.Wait();
                return t.Result;
            }
        }

        internal async System.Threading.Tasks.Task<bool> ZipDeployAsync(string zipToPublishPath, string userName, string password, string publishUrl, string siteName, IHttpClient client, bool logMessages)
        {
            if (!File.Exists(zipToPublishPath) || client == null)
            {
                return false;
            }

            string zipDeployPublishUrl = null;

            if(!string.IsNullOrEmpty(publishUrl))
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

            Uri uri = new Uri(zipDeployPublishUrl, UriKind.Absolute);
            FileStream stream = File.OpenRead(zipToPublishPath);
            IHttpResponse response = await client.PostWithBasicAuthAsync(uri, userName, password, "application/zip", Encoding.UTF8, stream);
            if(response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                if(logMessages)
                {
                    Log.LogError(String.Format(Resources.ZipDeployFailureErrorMessage, zipDeployPublishUrl, response.StatusCode));
                }

                return false;
            }

            return true;
        }
    }
}

