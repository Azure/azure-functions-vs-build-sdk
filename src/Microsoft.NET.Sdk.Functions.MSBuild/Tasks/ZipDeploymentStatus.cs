using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.MSBuild.Properties;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.NET.Sdk.Functions.MSBuild.Tasks
{
    internal class ZipDeploymentStatus
    {
        private const int MaxMinutesToWait = 3;
        private const int StatusRefreshDelaySeconds = 3;
        private const int RetryCount = 3;
        private const int RetryDelaySeconds = 1;

        private readonly IHttpClient _client;
        private readonly string _userAgent;
        private readonly TaskLoggingHelper _log;
        private readonly bool _logMessages;

        public ZipDeploymentStatus(IHttpClient client, string userAgent, TaskLoggingHelper log, bool logMessages)
        {
            _client = client;
            _userAgent = userAgent;
            _log = log;
            _logMessages = logMessages;
        }

        public async Task<DeployStatus> PollDeploymentStatus(string deploymentUrl, string userName, string password)
        {
            DeployStatus deployStatus = DeployStatus.Pending;
            var tokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(MaxMinutesToWait));
            if (_logMessages)
            {
                _log.LogMessage(Resources.DeploymentStatusPolling);
            }
            while (!tokenSource.IsCancellationRequested && deployStatus != DeployStatus.Success && deployStatus != DeployStatus.Failed && deployStatus != DeployStatus.Unknown)
            {
                try
                {
                    deployStatus = await GetDeploymentStatus(deploymentUrl, userName, password, RetryCount, TimeSpan.FromSeconds(RetryDelaySeconds));
                    if (_logMessages)
                    {
                        _log.LogMessage(String.Format(Resources.DeploymentStatus, Enum.GetName(typeof(DeployStatus), deployStatus)));
                    }
                }
                catch (HttpRequestException)
                {
                    return DeployStatus.Unknown;
                }

                await Task.Delay(TimeSpan.FromSeconds(StatusRefreshDelaySeconds));
            }

            return deployStatus;
        }

        private async Task<DeployStatus> GetDeploymentStatus(string deploymentUrl, string userName, string password, int retryCount, TimeSpan retryDelay)
        {
            Dictionary<string, string> json = await InvokeGetRequestWithRetry<Dictionary<string, string>>(deploymentUrl, userName, password, retryCount, retryDelay);
            string statusString = null;
            if (json!= null && !json.TryGetValue("status", out statusString))
            {
                return DeployStatus.Unknown;
            }

            if (Enum.TryParse(statusString, out DeployStatus result))
            {
                return result;
            }

            return DeployStatus.Unknown;
        }

        private async Task<T> InvokeGetRequestWithRetry<T>(string url, string userName, string password, int retryCount, TimeSpan retryDelay)
        {
            IHttpResponse response = null;
            await Retry(async () =>
            {
                response = await _client.GetWithBasicAuthAsync(new Uri(url, UriKind.RelativeOrAbsolute), userName, password, _userAgent);
            }, retryCount, retryDelay);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                return default(T);
            }
            else
            {
                using (var stream = await response.GetResponseBodyAsync())
                {
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                }
            }
        }

        private async Task Retry(Func<Task> func, int retryCount, TimeSpan retryDelay)
        {
            while (true)
            {
                try
                {
                    await func();
                    return;
                }
                catch (Exception e)
                {
                    if (retryCount <= 0)
                    {
                        throw e;
                    }
                    retryCount--;
                }

                await Task.Delay(retryDelay);
            }
        }
    }
}
