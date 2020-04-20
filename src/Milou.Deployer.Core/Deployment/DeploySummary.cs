using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment
{
    public class DeploySummary
    {
        public TimeSpan TotalTime { get; set; }

        public List<string> DeletedFiles { get; } = new List<string>();

        public List<string> DeletedDirectories { get; } = new List<string>();

        public List<string> CreatedDirectories { get; } = new List<string>();

        public List<string> CreatedFiles { get; private set; } = new List<string>();

        public List<string> IgnoredFiles { get; } = new List<string>();

        public List<string> IgnoredDirectories { get; } = new List<string>();

        public List<string> UpdatedFiles { get; } = new List<string>();

        public int ExitCode { get; set; }

        public void Add([NotNull] DeploySummary other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            DeletedFiles.AddRange(other.DeletedFiles);
            DeletedDirectories.AddRange(other.DeletedDirectories);
            CreatedDirectories.AddRange(other.CreatedDirectories);
            UpdatedFiles.AddRange(other.UpdatedFiles);
            CreatedFiles.AddRange(other.CreatedFiles);
            CreatedFiles = CreatedFiles.Except(UpdatedFiles).ToList();
            IgnoredFiles.AddRange(other.IgnoredFiles);
            IgnoredDirectories.AddRange(other.IgnoredDirectories);
        }

        public string ToDisplayValue()
        {
            var builder = new StringBuilder();

            var updatedDirectories = CreatedDirectories.Intersect(DeletedDirectories).ToImmutableArray();

            if (updatedDirectories.Length > 0)
            {
                builder.AppendLine("Updated directories:");
                foreach (string updateDirectory in updatedDirectories)
                {
                    builder.AppendLine("* " + updateDirectory);
                }
            }

            if (CreatedFiles.Count > 0)
            {
                builder.AppendLine("Created files:");
                foreach (string createdFile in CreatedFiles)
                {
                    builder.AppendLine("* " + createdFile);
                }
            }

            if (UpdatedFiles.Count > 0)
            {
                builder.AppendLine("Updated files:");
                foreach (string updatedFile in UpdatedFiles)
                {
                    builder.AppendLine("* " + updatedFile);
                }
            }

            if (DeletedFiles.Count > 0)
            {
                builder.AppendLine("Deleted files:");
                foreach (string deletedFile in DeletedFiles)
                {
                    builder.AppendLine("* " + deletedFile);
                }
            }

            builder.AppendLine("Ignored files: " + IgnoredFiles.Count);
            builder.AppendLine("Created files: " + CreatedFiles.Count);
            builder.AppendLine("Updated files: " + UpdatedFiles.Count);
            builder.AppendLine("Deleted files: " + DeletedFiles.Count);

            builder.AppendLine($"Total time: {TotalTime.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} seconds");

            return builder.ToString();
        }
    }
}
