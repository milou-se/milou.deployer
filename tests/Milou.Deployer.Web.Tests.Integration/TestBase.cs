using System;
using System.Threading;
using Arbor.App.Extensions.Configuration;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public abstract class TestBase<T> : IDisposable, IClassFixture<T> where T : class, IAppHost
    {
        protected TestBase([NotNull] T webFixture, [NotNull] ITestOutputHelper output)
        {
            Output = output ?? throw new ArgumentNullException(nameof(output));
            WebFixture = webFixture ?? throw new ArgumentNullException(nameof(webFixture));
            webFixture.App?.ConfigurationInstanceHolder?.AddInstance(output);

            CancellationTokenSource = WebFixture?.App?.CancellationTokenSource;

            if (webFixture.Exception is {})
            {
                output.WriteLine(webFixture.Exception.ToString());
            }
        }

        protected T WebFixture { get; private set; }

        [PublicAPI]
        public ITestOutputHelper Output { get; }

        [PublicAPI]
        protected CancellationTokenSource CancellationTokenSource { get; }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
            Output?.WriteLine($"Disposing {nameof(TestBase<T>)}");

            if (CancellationTokenSource is {} && !CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    CancellationTokenSource.Cancel(false);
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
            }

            if (WebFixture is {})
            {
                Output?.WriteLine($"Disposing {WebFixture}");

                WebFixture.App?.Dispose();

                if (WebFixture is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                if (WebFixture is IAsyncLifetime lifeTime)
                {
                    lifeTime.DisposeAsync().Wait();
                }

                if (WebFixture is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
                }

                WebFixture = null!;
            }
        }
    }
}