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
        private Site _site;
        private ServerManager _serverManager;

        public static IISManager Create(DeployerConfiguration configuration, ILogger logger)
        {
            return new IISManager(new ServerManager(),configuration, logger);
        }

        private IISManager(ServerManager serverManager, DeployerConfiguration configuration, ILogger logger)
        {
            _serverManager = serverManager;
            _configuration = configuration;
            _logger = logger;
        }

        public void Dispose()
        {
            RestoreState();
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
                    _logger.Debug("Starting IIS site '{IISSiteName}'", _site.Name);
                    _site.Start();
                    _logger.Debug("Starting IIS site '{IISSiteName}'", _site.Name);
                }
            }
            finally
            {
                _serverManager?.Dispose();
                _serverManager = null;
                _site = null;
            }
        }

        public void StopSiteIfApplicable(DeploymentExecutionDefinition deploymentExecutionDefinition)
        {
            _site = null;
            _previousSiteState = ObjectState.Unknown;

            if (_serverManager is null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(deploymentExecutionDefinition.IisSitename))
                {
                    if (_configuration.StopStartIisWebSiteEnabled)
                    {
                        _site = _serverManager.Sites[deploymentExecutionDefinition.IisSitename];

                        if (_site.HasValue())
                        {
                            _previousSiteState = _site.State;
                            if (_previousSiteState == ObjectState.Starting
                                || _previousSiteState == ObjectState.Started)
                            {
                                _logger.Debug("Stopping IIS site '{IISSiteName}'", _site.Name);
                                _site.Stop();
                                _logger.Debug("Stopped IIS site '{IISSiteName}'", _site.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while trying to stop IIS site");
            }
        }
    }
}
