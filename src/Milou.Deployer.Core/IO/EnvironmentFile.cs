using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Milou.Deployer.Core.IO
{
    public sealed class EnvironmentFile
    {
        public EnvironmentFile(FileInfo fileInfo, IEnumerable<string> fileNameParts)
        {
            File = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
            FileNameParts = fileNameParts?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(fileNameParts));
        }

        public FileInfo File { get; }

        public ImmutableArray<string> FileNameParts { get; }
    }
}