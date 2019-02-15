using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Milou.Deployer.Core.Deployment
{
    public class ReplaceResult
    {
        public ReplaceResult(bool isSuccess)
            : this(isSuccess,
                Array.Empty<string>())
        {
        }

        public ReplaceResult(bool isSuccess, IEnumerable<string> replacedFiles)
        {
            IsSuccess = isSuccess;
            ReplacedFiles = replacedFiles?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(replacedFiles));
        }

        public bool IsSuccess { get; }

        public ImmutableArray<string> ReplacedFiles { get; }
    }
}
