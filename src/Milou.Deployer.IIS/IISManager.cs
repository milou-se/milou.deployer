using System;
using JetBrains.Annotations;
using Microsoft.Web.Administration;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;
using Serilog;

namespace Milou.Deployer.IIS
{
    [UsedImplicitly]
    public sealed class IISManager : IIISManager
    {
        private readonly DeployerConfiguration _configuration;
        private readonly ILogger _logger;
        private ObjectState _previousSiteState;
        private ServerManager _serverManager;
        private Site _site;

        private IISManager(ServerManager serverManager, DeployerConfiguration configuration, ILogger logger)
        {
            _serverManager = serverManager;
            _configuration = configuration;
            _logger = logger;
        }

        public static IISManager Create([NotNull] DeployerConfiguration configuration, [NotNull] ILogger logger)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new IISManager(new ServerManager(), configuration, logger);
        }

        public void Dispose()
        {
            bool restored = RestoreState();

            if (restored)
            {
                _logger.Debug("Restored iis site state");
            }
            else
            {
                _logger.Debug("Failed to restore iis site state");
            }
        }

        public bool StopSiteIfApplicable([NotNull] DeploymentExecutionDefinition deploymentExecutionDefinition)
        {
            if (deploymentExecutionDefinition == null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            if (!UserHelper.IsAdministrator())
            {
                _logger.Warning("Current user does not have administrative privileges, cannot start/stop site");
                return false;
            }

            _site = null;
            _previousSiteState = ObjectState.Unknown;

            if (_serverManager is null)
            {
                _logger.Error(
                    "There is no ServerManager instance when trying to stop IIS site defined in {DeploymentExecutionDefinition}",
                    deploymentExecutionDefinition);
                return false;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(deploymentExecutionDefinition.IisSiteName))
                {
                    _logger.Debug(
                        "The deployment execution definition {DeploymentExecutionDefinition} has no site named defined",
                        deploymentExecutionDefinition);
                    return false;
                }

                if (!_configuration.StopStartIisWebSiteEnabled)
                {
                    _logger.Warning("The deployer configuration has {Property} set to false",
                        nameof(_configuration.StopStartIisWebSiteEnabled));
                    return false;
                }

                _site = _serverManager.Sites[deploymentExecutionDefinition.IisSiteName];

                if (!_site.HasValue())
                {
                    _logger.Error(
                        "Could not find IIS site {SiteName} defined in deployment execution definition {DeploymentExecutionDefinition}",
                        deploymentExecutionDefinition.IisSiteName,
                        deploymentExecutionDefinition);
                    return false;
                }

                _previousSiteState = _site.State;

                if (_previousSiteState == ObjectState.Starting
                    || _previousSiteState == ObjectState.Started)
                {
                    _logger.Debug("Running stop IIS site '{IISSiteName}'", _site.Name);
                    ObjectState objectState = _site.Stop();
                    if (objectState == ObjectState.Stopped)
                    {
                        _logger.Debug("Stopped IIS site '{IISSiteName}'", _site.Name);
                    }

                    if (objectState == ObjectState.Stopping)
                    {
                        _logger.Debug("Stopping IIS site '{IISSiteName}'", _site.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while trying to stop IIS site");
                return false;
            }

            return true;
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
                    _logger.Debug("Running start IIS site '{IISSiteName}'", _site.Name);
                    ObjectState objectState = _site.Start();

                    if (objectState == ObjectState.Started)
                    {
                        _logger.Debug("Started IIS site '{IISSiteName}'", _site.Name);
                    }

                    if (objectState == ObjectState.Starting)
                    {
                        _logger.Debug("Starting IIS site '{IISSiteName}'", _site.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not restart site {IISSiteName}", _site.Name);
            }
            finally
            {
                _serverManager?.Dispose();
                _serverManager = null;
                _site = null;
            }

            return true;
        }
    }
}
