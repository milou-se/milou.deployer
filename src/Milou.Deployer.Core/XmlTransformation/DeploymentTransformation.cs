using System;
using System.IO;
using JetBrains.Annotations;

using Microsoft.Web.XmlTransform;

using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;
using Serilog;

namespace Milou.Deployer.Core.XmlTransformation
{
    public class DeploymentTransformation
    {
        public static void Transform([NotNull] DeploymentExecutionDefinition deploymentExecutionDefinition,
            [NotNull] DirectoryInfo contentDirectory,
            [NotNull] ILogger logger)
        {
            if (deploymentExecutionDefinition == null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            if (contentDirectory == null)
            {
                throw new ArgumentNullException(nameof(contentDirectory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {
                logger.Debug(
                    "Found web config transformation {Transformation} for deployment execution definition {Deployment}",
                    deploymentExecutionDefinition.WebConfigTransformFile,
                    deploymentExecutionDefinition);

                var transformFile = new FileInfo(deploymentExecutionDefinition.WebConfigTransformFile);

                if (transformFile.Exists)
                {
                    string tempFileName = Path.GetTempFileName();

                    var webConfig = new FileInfo(Path.Combine(contentDirectory.FullName, "web.config"));

                    if (webConfig.Exists)
                    {
                        using (var x = new XmlTransformableDocument())
                        {
                            x.PreserveWhitespace = true;
                            x.Load(webConfig.FullName);

                            using (var transform = new Microsoft.Web.XmlTransform.XmlTransformation(transformFile.FullName))
                            {
                                bool succeed = transform.Apply(x);

                                if (succeed)
                                {
                                    using (var fsDestFileStream =
                                        new FileStream(tempFileName, FileMode.OpenOrCreate))
                                    {
                                        x.Save(fsDestFileStream);
                                    }
                                }
                            }
                        }
                    }

                    var tempFileInfo = new FileInfo(tempFileName);

                    if (tempFileInfo.Exists && tempFileInfo.Length > 0)
                    {
                        logger.Information(
                            "Successfully transformed web.config with transformation {Transformation}",
                            deploymentExecutionDefinition.WebConfigTransformFile);
                        tempFileInfo.CopyTo(webConfig.FullName, overwrite: true);
                    }
                    else
                    {
                        logger.Warning(
                            "Failed to transform web.config with transformation {Transformation}",
                            deploymentExecutionDefinition.WebConfigTransformFile);
                    }

                    tempFileInfo.Delete();
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error(ex, "Could not apply web.config transform with {Transform}", deploymentExecutionDefinition.WebConfigTransformFile);
                throw;
            }
        }
    }
}
