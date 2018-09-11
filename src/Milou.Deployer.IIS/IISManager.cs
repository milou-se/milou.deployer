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
            RestoreState();
        }

        public void StopSiteIfApplicable([NotNull] DeploymentExecutionDefinition deploymentExecutionDefinition)
        {
            if (deploymentExecutionDefinition == null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            _site = null;
            _previousSiteState = ObjectState.Unknown;

            if (_serverManager is null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(deploymentExecutionDefinition.IisSiteName))
                {
                    return;
                }

                if (!_configuration.StopStartIisWebSiteEnabled)
                {
                    return;
                }

                _site = _serverManager.Sites[deploymentExecutionDefinition.IisSiteName];

                if (!_site.HasValue())
                {
                    return;
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
            }
        }

        private void RestoreState()
        {
            try
            {
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
            finally
            {
                _serverManager?.Dispose();
                _serverManager = null;
                _site = null;
            }
        }
    }
}
