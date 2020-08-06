using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Time;
using MediatR;
using Microsoft.AspNetCore.Http;
using Milou.Deployer.Web.Core.NuGet;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.WebHooks
{
    public class PackageWebHookHandler
    {
        private readonly ILogger _logger;

        private readonly IMediator _mediator;

        private readonly ImmutableArray<IPackageWebHook> _packageWebHooks;
        private readonly TimeoutHelper _timeoutHelper;

        public PackageWebHookHandler(
            IEnumerable<IPackageWebHook> packageWebHooks,
            ILogger logger,
            IMediator mediator,
            TimeoutHelper timeoutHelper)
        {
            _logger = logger;
            _mediator = mediator;
            _timeoutHelper = timeoutHelper;
            _packageWebHooks = packageWebHooks.SafeToImmutableArray();
        }

        public async Task<WebHookResult> HandleRequest(
            HttpRequest request,
            string content,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.Debug("Cannot process empty web hook request body");
                return new WebHookResult(false);
            }

            bool handled = false;

            foreach (IPackageWebHook packageWebHook in _packageWebHooks)
            {
                CancellationTokenSource? cancellationTokenSource = default;

                if (cancellationToken == CancellationToken.None)
                {
                    cancellationTokenSource = _timeoutHelper.CreateCancellationTokenSource(TimeSpan.FromSeconds(10));
                    cancellationToken = cancellationTokenSource.Token;
                }

                try
                {
                    PackageUpdatedEvent? webHook =
                        await packageWebHook.TryGetWebHookNotification(request, content, cancellationToken);

                    if (webHook is null)
                    {
                        continue;
                    }

                    handled = true;

                    _logger.Information("Web hook successfully handled by {Handler}",
                        packageWebHook.GetType().FullName);

                    await Task.Run(() => _mediator.Publish(webHook, cancellationToken), cancellationToken);

                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Could not get web hook notification from hook {Hook}",
                        packageWebHook.GetType().FullName);
                    throw;
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                }
            }

            return new WebHookResult(handled);
        }
    }
}