﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Time;
using Arbor.KVConfiguration.Core;
using Arbor.Tooler;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Core.Configuration;
using Milou.Deployer.Web.Core.NuGet;
using Milou.Deployer.Web.Core.Startup;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.NuGet
{
    [UsedImplicitly]
    public class NuGetDownloadStartupTask : BackgroundService, IStartupTask
    {
        private readonly IKeyValueConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly NuGetConfiguration? _nugetConfiguration;
        private readonly TimeoutHelper _timeoutHelper;

        public NuGetDownloadStartupTask(ILogger logger,
            IKeyValueConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            TimeoutHelper timeoutHelper,
            NuGetConfiguration? nugetConfiguration = null)
        {
            _logger = logger;
            _configuration = configuration;
            _nugetConfiguration = nugetConfiguration;
            _httpClientFactory = httpClientFactory;
            _timeoutHelper = timeoutHelper;
        }

        public bool IsCompleted { get; private set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            string? nugetExePath = "";

            _logger.Debug("Ensuring nuget.exe exists");

            if (!int.TryParse(_configuration[DeployerAppConstants.NuGetDownloadTimeoutInSeconds],
                out int initialNuGetDownloadTimeoutInSeconds) || initialNuGetDownloadTimeoutInSeconds <= 0)
            {
                initialNuGetDownloadTimeoutInSeconds = 100;
            }

            try
            {
                var fromSeconds = TimeSpan.FromSeconds(initialNuGetDownloadTimeoutInSeconds);

                using CancellationTokenSource cts = _timeoutHelper.CreateCancellationTokenSource(fromSeconds);
                string? downloadDirectory = _configuration[DeployerAppConstants.NuGetExeDirectory].WithDefault();
                string? exeVersion = _configuration[DeployerAppConstants.NuGetExeVersion].WithDefault();

                HttpClient httpClient = _httpClientFactory.CreateClient();

                var nuGetDownloadClient = new NuGetDownloadClient();
                NuGetDownloadResult nuGetDownloadResult = await nuGetDownloadClient.DownloadNuGetAsync(
                    new NuGetDownloadSettings(downloadDirectory: downloadDirectory, nugetExeVersion: exeVersion),
                    _logger,
                    httpClient,
                    cts.Token);

                if (nuGetDownloadResult.Succeeded)
                {
                    nugetExePath = nuGetDownloadResult.NuGetExePath;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Could not download nuget.exe");
            }

            if (_configuration is { } && _nugetConfiguration is {})
            {
                _nugetConfiguration.NugetExePath = nugetExePath;
            }

            IsCompleted = true;
        }
    }
}