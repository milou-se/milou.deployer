using System;
using System.Security.Principal;

namespace Milou.Deployer.IIS
{
    public static class UserHelper
    {
        public static bool IsAdministrator()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
#pragma warning restore CA1416 // Validate platform compatibility
            }

            return false;
        }
    }
}