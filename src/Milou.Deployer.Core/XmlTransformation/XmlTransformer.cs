using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Arbor.Processing;

using Microsoft.Web.XmlTransform;

using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.IO;
using Serilog;

namespace Milou.Deployer.Core.XmlTransformation
{
    public sealed class XmlTransformer
    {
        private readonly FileMatcher _fileMatcher;
        private readonly ILogger _logger;

        public XmlTransformer(ILogger logger, FileMatcher fileMatcher)
        {
            _logger = logger;
            _fileMatcher = fileMatcher;
        }

        public ExitCode TransformFile(
            FileInfo originalFile,
            FileInfo transformationFile,
            DirectoryInfo originalFileRootDirectory,
            DirectoryInfo transformationFileRootDirectory)
        {
#if !DOTNET5_4

            if (!originalFile.Exists)
            {
                _logger.Error("The original file to transform '{FullName}' does not exist", originalFile.FullName);
                return ExitCode.Failure;
            }

            if (!transformationFile.Exists)
            {
                _logger.Error(
                    "The transformation file '{FullName}' to transform '{FullName1}' does not exist",
                    transformationFile.FullName,
                    originalFile.FullName);
                return ExitCode.Failure;
            }

            string destFilePath = Path.GetTempFileName();

            _logger.Debug(
                "Transforming original '{FullName}' with transformation '{FullName1}' using temp target '{DestFilePath}'",
                originalFile.FullName,
                transformationFile.FullName,
                destFilePath);

            var xmlTransformableDocument =
                new XmlTransformableDocument { PreserveWhitespace = true };
            xmlTransformableDocument.Load(originalFile.FullName);

            bool succeed;
            using (
                var transform =
                    new Microsoft.Web.XmlTransform.XmlTransformation(transformationFile.FullName))
            {
                succeed = transform.Apply(xmlTransformableDocument);
            }

            if (!succeed)
            {
                _logger.Error(
                    "Transforming failed, original '{FullName}' with transformation '{FullName1}' using temp target '{DestFilePath}'",
                    originalFile.FullName,
                    transformationFile.FullName,
                    destFilePath);
                return ExitCode.Failure;
            }

            _logger.Debug(
                "Transforming succeeded, original '{FullName}' with transformation '{FullName1}' using temp target '{DestFilePath}'",
                originalFile.FullName,
                transformationFile.FullName,
                destFilePath);

            using (var destinationFileStream = new FileStream(destFilePath, FileMode.OpenOrCreate))
            {
                xmlTransformableDocument.Save(destinationFileStream);
            }

            File.Copy(destFilePath, originalFile.FullName, true);

            _logger.Debug(
                "Rewritten original '{FullName}' with transformation '{FullName1}' using temp target '{DestFilePath}'",
                originalFile.FullName,
                transformationFile.FullName,
                destFilePath);

            string originalRelativePath = originalFile.GetRelativePath(originalFileRootDirectory);
            string transformRelativePath = transformationFile.GetRelativePath(transformationFileRootDirectory);

            _logger.Information(
                "Transformed original '{OriginalRelativePath}' with transformation '{TransformRelativePath}'",
                originalRelativePath,
                transformRelativePath);

            if (File.Exists(destFilePath))
            {
                File.Delete(destFilePath);
                _logger.Debug("Deleted temp transformation destination file '{DestFilePath}'", destFilePath);
            }
#endif

#if DOTNET5_4
            _logger.Error($"Transforming is not yet supported using DNXCORE");

            return ExitCode.Failure;
#endif

            return ExitCode.Success;
        }

        public TransformationResult TransformMatch(FileMatch possibleXmlTransformation, DirectoryInfo contentDirectory)
        {
            ImmutableArray<FileInfo> matchingFiles = _fileMatcher.Matches(
                possibleXmlTransformation,
                contentDirectory);

            var transformedFiles = new List<string>();

            if (matchingFiles.Length > 1)
            {
                _logger.Error(
                    "Could not find a single matching file to transform, found multiple: {V}",
                    string.Join(", ", matchingFiles.Select(file => $"'{file.FullName}'")));
                return new TransformationResult(false);
            }

            if (matchingFiles.Any())
            {
                FileInfo originalFile = matchingFiles.Single();

                ExitCode transformExitCode = TransformFile(
                    originalFile,
                    possibleXmlTransformation.ActionFile,
                    contentDirectory,
                    possibleXmlTransformation.ActionFileRootDirectory);

                if (!transformExitCode.IsSuccess)
                {
                    return new TransformationResult(false);
                }

                transformedFiles.Add(originalFile.Name);
            }
            else
            {
                _logger.Debug(
                    "Could not find any matching file for transform, looked for '{TargetName}'",
                    possibleXmlTransformation.TargetName);
            }

            return new TransformationResult(true, transformedFiles);
        }
    }
}
