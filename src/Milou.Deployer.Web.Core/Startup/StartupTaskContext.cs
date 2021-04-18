using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arbor.App.Extensions.ExtensionMethods;
using Serilog;

namespace Milou.Deployer.Web.Core.Startup
{
    public class StartupTaskContext
    {
        private readonly ImmutableArray<IStartupTask> _startupTasks;

        private bool _isCompleted;
        private readonly ILogger _logger;

        public StartupTaskContext(IEnumerable<IStartupTask> startupTasks, ILogger logger)
        {
            _logger = logger;
            _startupTasks = startupTasks.SafeToImmutableArray();
        }

        public bool IsCompleted
        {
            get
            {
                if (_isCompleted)
                {
                    return true;
                }

                var pendingStartupTasks = _startupTasks.Where(task => !task.IsCompleted)
                    .Select(task => task.ToString())
                    .ToArray();

                _isCompleted = pendingStartupTasks.Length == 0;

                if (!_isCompleted)
                {
                    _logger.Debug("Waiting for startup tasks {Tasks}", string.Join(", ", pendingStartupTasks));
                }
                else if (_startupTasks.Length > 0)
                {
                    _logger.Debug("All startup tasks are completed");
                }

                return _isCompleted;
            }
        }
    }
}