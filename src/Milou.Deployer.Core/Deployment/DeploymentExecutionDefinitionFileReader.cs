using System;
using System.IO;
using System.Text;

namespace Milou.Deployer.Core.Deployment
{
    public static class DeploymentExecutionDefinitionFileReader
    {
        public static string ReadAllData(string manifestFilePath)
        {
            if (string.IsNullOrWhiteSpace(manifestFilePath))
            {
                throw new ArgumentNullException(nameof(manifestFilePath));
            }

            if (!File.Exists(manifestFilePath))
            {
                throw new InvalidOperationException($"The manifest file '{manifestFilePath}' does not exist");
            }

            var fileInfo = new FileInfo(manifestFilePath);

            if (fileInfo.Length == 0)
            {
                throw new InvalidOperationException($"The manifest file '{manifestFilePath}' has length 0");
            }

            string data = File.ReadAllText(manifestFilePath, Encoding.UTF8);

            return data;
        }
    }
}