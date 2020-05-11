using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Milou.Deployer.Core.XmlTransformation
{
    public class TransformationResult
    {
        public TransformationResult(bool isSuccess) : this(isSuccess, Array.Empty<string>())
        {
        }

        public TransformationResult(bool isSuccess, IEnumerable<string> transformedFiles)
        {
            if (transformedFiles is null)
            {
                throw new ArgumentNullException(nameof(transformedFiles));
            }

            IsSuccess = isSuccess;
            TransformedFiles = transformedFiles.ToImmutableArray();
        }

        public bool IsSuccess { get; }

        public ImmutableArray<string> TransformedFiles { get; }
    }
}