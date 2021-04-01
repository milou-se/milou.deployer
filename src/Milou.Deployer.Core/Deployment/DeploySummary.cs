using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Arbor.App.Extensions.ExtensionMethods;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment
{
    public class DeploySummary
    {
        public TimeSpan TotalTime { get; set; }

        public HashSet<string> DeletedFiles { get; } = new();

        public HashSet<string> DeletedDirectories { get; } = new();

        public HashSet<string> CreatedDirectories { get; } = new();

        public HashSet<string> CreatedFiles { get; private set; } = new();

        public HashSet<string> IgnoredFiles { get; } = new();

        public HashSet<string> IgnoredDirectories { get; } = new();

        public HashSet<string> UpdatedFiles { get; } = new();

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
            CreatedFiles = CreatedFiles.Except(UpdatedFiles).ToHashSet();
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

            builder.AppendLine(
                $"Total time: {TotalTime.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} seconds");

            return builder.ToString();
        }
    }
}