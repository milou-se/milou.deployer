using System.IO;

using Arbor.Aesculus.Core;

using NCrunch.Framework;

namespace Milou.Deployer.Tests.Integration
{
    public class VcsTestPathHelper
    {
        public static string FindVcsRootPath()
        {
            if (NCrunchEnvironment.NCrunchIsResident())
            {
                DirectoryInfo directory = new FileInfo(NCrunchEnvironment.GetOriginalSolutionPath()).Directory;

                return VcsPathHelper.FindVcsRootPath(directory?.FullName);
            }

            return VcsPathHelper.FindVcsRootPath();
        }
    }
}