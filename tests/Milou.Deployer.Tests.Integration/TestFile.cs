using System.Collections.Generic;
using System.IO;

namespace Milou.Deployer.Tests.Integration
{
    public static class TestFile
    {
        public static string GetTestFile(string path, params string[] paths)
        {
            var allPaths = new List<string>
            {
                VcsTestPathHelper.FindVcsRootPath(), "tests", "Milou.Deployer.Tests.Integration", path
            };
            allPaths.AddRange(paths);
            return Path.Combine(allPaths.ToArray());
        }
    }
}