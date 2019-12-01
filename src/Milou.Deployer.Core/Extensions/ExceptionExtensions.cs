using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Milou.Deployer.Core.Extensions
{
    public static class ExceptionExtensions
    {
        public static bool IsFatal(this Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            bool isFatal = ex is OutOfMemoryException ||
                           ex is AccessViolationException
                           || ex is AppDomainUnloadedException
                           || ex is StackOverflowException
                           || ex is ThreadAbortException ||
                           ex is SEHException;

            return isFatal;
        }
    }
}
