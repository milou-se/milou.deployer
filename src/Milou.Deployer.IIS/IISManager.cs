using System;
using JetBrains.Annotations;
using Microsoft.Web.Administration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Configuration;
using Milou.Deployer.Core.Extensions;
using Serilog;
using Serilog.Events;

namespace Milou.Deployer.IIS
{
    [UsedImplicitly]
    public sealed class IisManager : IIisManager
    {
        private readonly DeployerConfiguration _configuration;
        private readonly DeploymentExecutionDefinition _deploymentExecutionDefinition;
        private readonly ILogger _logger;
        private ObjectState _previousSiteState;
        private ServerManager _serverManager;
        private Site _site;

        private IisManager(
            ServerManager serverManager,
            DeployerConfiguration configuration,
            ILogger logger,
            DeploymentExecutionDefinition deploymentExecutionDefinition)
        {
            _serverManager = serverManager;
            _configuration = configuration;
            _logger = logger;
            _deploymentExecutionDefinition = deploymentExecutionDefinition;
        }

        public void Dispose()
        {
            bool restored = RestoreState();

            if (!_logger.IsEnabled(LogEventLevel.Debug))
            {
                return;
            }

            if (restored)
            {
                _logger.Debug(
                    "Restored iis site state to {State} for site {SiteName} defined in deployment execution definition {DeploymentExecutionDefinition}",
                    _site.State,
                    _deploymentExecutionDefinition.IisSiteName,
                    _deploymentExecutionDefinition);
            }
            else
            {
                _logger.Debug(
                    "Failed to restore iis site state for site {SiteName} defined in deployment execution definition {DeploymentExecutionDefinition}",
                    _deploymentExecutionDefinition.IisSiteName,
                    _deploymentExecutionDefinition);
            }
        }

        public bool StopSiteIfApplicable()
        {
            if (!UserHelper.IsAdministrator())
            {
                _logger.Warning("Current user does not have administrative privileges, cannot start/stop site");
                return false;
            }

            _site = null!;
            _previousSiteState = ObjectState.Unknown;

            if (_serverManager is null)
            {
                _logger.Error(
                    "There is no ServerManager instance when trying to stop IIS site defined in {DeploymentExecutionDefinition}",
                    _deploymentExecutionDefinition);
                return false;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(_deploymentExecutionDefinition.IisSiteName))
                {
                    if (_logger.IsEnabled(LogEventLevel.Debug))
                    {
                        _logger.Debug(
                            "The deployment execution definition {DeploymentExecutionDefinition} has no site named defined",
                            _deploymentExecutionDefinition);
                    }

                    return false;
                }

                if (!_configuration.StopStartIisWebSiteEnabled)
                {
                    _logger.Warning(
                        "The deployer configuration has {Property} set to false",
                        nameof(_configuration.StopStartIisWebSiteEnabled));
                    return false;
                }

                _site = _serverManager.Sites[_deploymentExecutionDefinition.IisSiteName];

                if (!_site.HasValue())
                {
                    _logger.Error(
                        "Could not find IIS site {SiteName} defined in deployment execution definition {DeploymentExecutionDefinition}",
                        _deploymentExecutionDefinition.IisSiteName,
                        _deploymentExecutionDefinition);
                    return false;
                }

                _previousSiteState = _site.State;

                if (_previousSiteState == ObjectState.Starting
                    || _previousSiteState == ObjectState.Started)
                {
                    if (_logger.IsEnabled(LogEventLevel.Debug))
                    {
                        _logger.Debug("Running stop IIS site '{IISSiteName}'", _site.Name);
                    }

                    var objectState = _site.Stop();
                    if (objectState == ObjectState.Stopped && _logger.IsEnabled(LogEventLevel.Debug))
                    {
                        _logger.Debug("Stopped IIS site '{IISSiteName}'", _site.Name);
                    }

                    if (objectState == ObjectState.Stopping && _logger.IsEnabled(LogEventLevel.Debug))
                    {
                        _logger.Debug("Stopping IIS site '{IISSiteName}'", _site.Name);
                    }
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Error while trying to stop IIS site");
                return false;
            }

            return true;
        }

        public static IisManager Create(
            [NotNull] DeployerConfiguration configuration,
            [NotNull] ILogger logger,
            [NotNull] DeploymentExecutionDefinition deploymentExecutionDefinition)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (deploymentExecutionDefinition is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            return new IisManager(new ServerManager(), configuration, logger, deploymentExecutionDefinition);
        }

        private bool RestoreState()
        {
            try
            {
                if (!UserHelper.IsAdministrator())
                {
                    _logger.Warning("Current user does not have administrative privileges, cannot start/stop site");
                    return false;
                }

                if (_serverManager.HasValue()
                    && _site.HasValue()
                    && _site.State != ObjectState.Starting
                    && _site.State != ObjectState.Started
                    && (_previousSiteState == ObjectState.Starting
                        || _previousSiteState == ObjectState.Started))
                {
                    if (_logger.IsEnabled(LogEventLevel.Debug))
                    {
                        _logger.Debug("Running start IIS site '{IISSiteName}'", _site.Name);
                    }

                    var objectState = _site.Start();

                    if (objectState == ObjectState.Started && _logger.IsEnabled(LogEventLevel.Debug))
                    {
                        _logger.Debug("Started IIS site '{IISSiteName}'", _site.Name);
                    }

                    if (objectState == ObjectState.Starting && _logger.IsEnabled(LogEventLevel.Debug))
                    {
                        _logger.Debug("Starting IIS site '{IISSiteName}'", _site.Name);
                    }
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not restart site {IISSiteName}", _site.Name);
            }
            finally
            {
                _serverManager?.Dispose();
                _serverManager = null!;
                _site = null!;
            }

            return true;
        }
    }
}