﻿using System;
using Arbor.App.Extensions;
using Xunit;

namespace Milou.Deployer.Web.Tests.Unit
{
    public class DisposeTest
    {
        private class TestDisposable : IDisposable
        {
            public void Dispose()
            {
                // ignore
            }
        }

        [Fact]
        public void DisposeDisposable()
        {
            var o = new TestDisposable();
            o.SafeDispose();
        }

        [Fact]
        public void DisposeNonDisposable()
        {
            object o = new object();
            o.SafeDispose();
        }

        [Fact]
        public void DisposeNull()
        {
            object? o = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            o.SafeDispose();
        }
    }
}