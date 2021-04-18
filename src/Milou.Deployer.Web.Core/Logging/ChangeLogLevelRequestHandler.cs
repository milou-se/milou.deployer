using System;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using JetBrains.Annotations;
using MediatR;
using Serilog;

namespace Milou.Deployer.Web.Core.Logging
{
    [UsedImplicitly]
    public class ChangeLogLevelRequestHandler : IRequestHandler<ChangeLogLevelRequest>
    {
        private readonly LogLevelState _levelState;
        private readonly ILogger _logger;

        public ChangeLogLevelRequestHandler(LogLevelState levelState, ILogger logger)
        {
            _levelState = levelState;
            _logger = logger;
        }

        public Task<Unit> Handle([NotNull] ChangeLogLevelRequest? request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (LogEventLevelParser.TryParse(request.ChangeLogLevel.NewLevel, out var newLevel) && TimeSpan.TryParse(request.ChangeLogLevel.TimeSpan, out var timeSpan))
            {
                _levelState.SetLevel(newLevel, timeSpan);
            }
            else
            {
                _logger.Warning("Invalid log level request {Request}", request);
            }

            return Unit.Task;
        }
    }
}