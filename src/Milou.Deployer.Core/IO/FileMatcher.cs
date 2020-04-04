using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Milou.Deployer.Core.Extensions;
using Milou.Deployer.Core.XmlTransformation;

using Serilog;

namespace Milou.Deployer.Core.IO
{
    public sealed class FileMatcher
    {
        private readonly ILogger _logger;

        public FileMatcher(ILogger logger) => _logger = logger;

        public ImmutableArray<FileInfo> Matches([NotNull] FileMatch fileMatch, [NotNull] DirectoryInfo rootDirectory)
        {
            if (fileMatch is null)
            {
                throw new ArgumentNullException(nameof(fileMatch));
            }

            if (rootDirectory is null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }

            string filePattern = $"*{Path.GetExtension(fileMatch.TargetName)}";

            _logger.Debug(
                "Trying to find matches for '{TargetName}', looking in directory '{FullName}' recursively using file pattern '{FilePattern}'",
                fileMatch.TargetName,
                rootDirectory.FullName,
                filePattern);

            var matchingFiles =
                rootDirectory.GetFiles(filePattern, SearchOption.AllDirectories)
                    .Tap(
                        file =>
                            _logger.Debug("Found file '{FullName}' when trying to find matches for '{TargetName}'",
                                file.FullName,
                                fileMatch.TargetName))
                    .Where(
                        file =>
                        {
                            bool isMatch = file.Name.Equals(
                                fileMatch.TargetName,
                                StringComparison.OrdinalIgnoreCase);

                            _logger.Debug(
                                "Found file '{FullName}' matches: {IsMatch}, when trying to find matches for '{TargetName}'",
                                file.FullName,
                                isMatch,
                                fileMatch.TargetName);

                            return isMatch;
                        }).Where(
                        file =>
                        {
                            string sourceRelativePath =
                                fileMatch.ActionFile.Directory.GetRelativePath(
                                    fileMatch.ActionFileRootDirectory);
                            string targetFileRelativePath = file.Directory.GetRelativePath(rootDirectory);

                            bool hasSameRelativePath = targetFileRelativePath.Equals(
                                sourceRelativePath,
                                StringComparison.OrdinalIgnoreCase);

                            _logger.Debug(
                                "Matching path between '{SourceRelativePath}' and '{TargetFileRelativePath}': {HasSameRelativePath}",
                                sourceRelativePath,
                                targetFileRelativePath,
                                hasSameRelativePath);

                            return hasSameRelativePath;
                        }).ToList();

            return matchingFiles.ToImmutableArray();
        }
    }
}
