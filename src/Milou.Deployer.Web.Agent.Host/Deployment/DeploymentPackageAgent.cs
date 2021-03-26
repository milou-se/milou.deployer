﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Time;
using Arbor.Processing;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Milou.Deployer.Web.Agent.Host.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Http;

namespace Milou.Deployer.Web.Agent.Host.Deployment
{
    public class DeploymentPackageAgent : IDeploymentPackageAgent
    {
        private readonly IDeploymentPackageHandler _deploymentPackageHandler;
        private readonly DeploymentTaskPackageService _deploymentTaskPackageService;
        private readonly ILogger _logger;
        private readonly LogHttpClientFactory _logHttpClientFactory;
        private readonly TimeoutHelper _timeoutHelper;

        public DeploymentPackageAgent(
            TimeoutHelper timeoutHelper,
            ILogger logger,
            LogHttpClientFactory logHttpClientFactory,
            IDeploymentPackageHandler deploymentPackageHandler,
            DeploymentTaskPackageService deploymentTaskPackageService,
            AgentConfiguration agentConfiguration)
        {
            _timeoutHelper = timeoutHelper;
            _logger = logger;
            _logHttpClientFactory = logHttpClientFactory;
            _deploymentPackageHandler = deploymentPackageHandler;
            _deploymentTaskPackageService = deploymentTaskPackageService;
            AgentId = agentConfiguration.AgentId();
        }

        public async Task<ExitCode> RunAsync(string deploymentTaskId,
            DeploymentTargetId deploymentTargetId,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Received deployment task {DeploymentTaskId}", deploymentTaskId);

            IHttpClient client = _logHttpClientFactory.CreateClient(deploymentTaskId, deploymentTargetId);

            Logger logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Logger(_logger)
                .WriteTo.DurableHttpUsingTimeRolledBuffers(AgentConstants.DeploymentTaskLogRoute,
                    period: TimeSpan.FromSeconds(1), httpClient: client)
                .CreateLogger(); //TODO create job logger in agent

            ExitCode exitCode;

            try
            {
                using CancellationTokenSource cancellationTokenSource =
                    _timeoutHelper.CreateCancellationTokenSource(TimeSpan.FromMinutes(30));

                var deploymentTaskPackage =
                    await _deploymentTaskPackageService.GetDeploymentTaskPackageAsync(deploymentTaskId,
                        cancellationTokenSource.Token);

                if (deploymentTaskPackage is null)
                {
                    _logger.Error("Could not get deployment task package for deployment task id {DeploymentTaskId}",
                        deploymentTaskId);

                    return ExitCode.Failure;
                }

                if (string.IsNullOrWhiteSpace(deploymentTaskPackage.DeploymentTaskId))
                {
                    _logger.Error(
                        "Deployment task package for deployment task id {DeploymentTaskId} is missing deployment task id",
                        deploymentTaskId);

                    return ExitCode.Failure;
                }

                exitCode =
                    await _deploymentPackageHandler.RunAsync(deploymentTaskPackage, logger,
                        cancellationTokenSource.Token);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Failed to deploy {DeploymentTaskId}", deploymentTaskId);
                return ExitCode.Failure;
            }

            return exitCode;
        }

        public AgentId AgentId { get; }
    }
}