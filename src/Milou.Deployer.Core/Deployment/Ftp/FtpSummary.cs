using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public class FtpSummary : IDeploymentChangeSummary
    {
        public TimeSpan TotalTime { get; set; }

        public List<string> Deleted { get; } = new List<string>();

        public List<string> DeletedDirectories { get; } = new List<string>();

        public List<string> CreatedDirectories { get; } = new List<string>();

        public List<string> CreatedFiles { get; private set; } = new List<string>();

        public List<string> IgnoredFiles { get; } = new List<string>();

        public List<string> IgnoredDirectories { get; } = new List<string>();

        public List<string> UpdatedFiles { get; } = new List<string>();

        public void Add([NotNull] FtpSummary other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Deleted.AddRange(other.Deleted);
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


            string[] updatedDirectories = CreatedDirectories.Intersect(DeletedDirectories).ToArray();


            //builder.AppendLine("Ignored directories: " + IgnoredDirectories.Count);
            //builder.AppendLine("Created directories: " + CreatedDirectories.Except(updatedDirectories).Count());
            //builder.AppendLine("Updated directories: " + updatedDirectories.Length);
            //builder.AppendLine("Deleted directories: " + DeletedDirectories.Except(updatedDirectories).Count());

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

            if (Deleted.Count > 0)
            {
                builder.AppendLine("Deleted files:");
                foreach (string deletedFile in Deleted)
                {
                    builder.AppendLine("* " + deletedFile);
                }
            }

            builder.AppendLine("Ignored files: " + IgnoredFiles.Count);
            builder.AppendLine("Created files: " + CreatedFiles.Count);
            builder.AppendLine("Updated files: " + UpdatedFiles.Count);
            builder.AppendLine("Deleted files: " + Deleted.Count);

            builder.AppendLine($"Total time: {TotalTime.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} seconds");

            return builder.ToString();
        }
    }
}
