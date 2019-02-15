using System;
using System.IO;
using System.Text;

namespace Milou.Deployer.Core.Deployment
{
    public class DeploymentExecutionDefinitionFileReader
    {
        public string ReadAllData(string manifestFilePath)
        {
            if (string.IsNullOrWhiteSpace(manifestFilePath))
            {
                throw new ArgumentNullException(nameof(manifestFilePath));
            }

            if (!File.Exists(manifestFilePath))
            {
                throw new InvalidOperationException($"The manifest file '{manifestFilePath}' does not exist");
            }

            string data = File.ReadAllText(manifestFilePath, Encoding.UTF8);

            return data;
        }
    }
}
