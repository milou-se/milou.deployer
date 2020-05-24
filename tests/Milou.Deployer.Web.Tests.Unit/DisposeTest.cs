using System;
using Arbor.App.Extensions.ExtensionMethods;
using Xunit;

namespace Milou.Deployer.Web.Tests.Unit
{
    public class DisposeTest
    {
        private class TestDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose() => IsDisposed = true;
        }

        [Fact]
        public void DisposeDisposable()
        {
            var o = new TestDisposable();
            o.SafeDispose();

            Assert.True(o.IsDisposed);
        }

        [Fact]
        public void DisposeNonDisposable()
        {
            Exception? exception = null;
            object o = new object();
            try
            {
                o.SafeDispose();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.Null(exception);
        }

        [Fact]
        public void DisposeNull()
        {
            object? o = null;

            Exception? exception = null;

            // ReSharper disable once ExpressionIsAlwaysNull
            try { o.SafeDispose(); }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.Null(exception);
        }
    }
}