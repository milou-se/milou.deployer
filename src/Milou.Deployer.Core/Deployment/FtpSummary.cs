using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Milou.Deployer.Core.Deployment
{
    public class FtpSummary : IDeploymentChangeSummary
    {
        public List<string> Deleted { get; } = new List<string>();

        public List<string> DeletedDirectories { get; } = new List<string>();

        public List<string> CreatedDirectories { get; } = new List<string>();

        public List<string> CreatedFiles { get; } = new List<string>();

        public List<string> IgnoredFiles { get; } = new List<string>();

        public List<string> IgnoredDirectories { get; } = new List<string>();

        public void Add(FtpSummary other)
        {
            Deleted.AddRange(other.Deleted);
            DeletedDirectories.AddRange(other.DeletedDirectories);
            CreatedDirectories.AddRange(other.CreatedDirectories);
            CreatedFiles.AddRange(other.CreatedFiles);
            IgnoredFiles.AddRange(other.IgnoredFiles);
            IgnoredDirectories.AddRange(other.IgnoredDirectories);
        }

        public string ToDisplayValue()
        {
            var builder = new StringBuilder();

            string[] updatedFiles = CreatedFiles.Intersect(Deleted).ToArray();
            string[] updatedDirectories = CreatedDirectories.Intersect(DeletedDirectories).ToArray();

            string[] createdFiles = CreatedFiles.Except(updatedFiles).ToArray();
            string[] deletedFiles = Deleted.Except(updatedFiles).ToArray();

            builder.AppendLine("Ignored files: " + IgnoredFiles.Count);
            builder.AppendLine("Created files: " + createdFiles.Length);
            builder.AppendLine("Updated files: " + updatedFiles.Length);
            builder.AppendLine("Deleted files: " + deletedFiles.Length);

            builder.AppendLine("Ignored directories: " + IgnoredDirectories.Count);
            builder.AppendLine("Created directories: " + CreatedDirectories.Except(updatedDirectories).Count());
            builder.AppendLine("Updated directories: " + updatedDirectories.Length);
            builder.AppendLine("Deleted directories: " + DeletedDirectories.Except(updatedDirectories).Count());

            if (createdFiles.Length > 0)
            {
                builder.AppendLine("Created files:");
                foreach (string createdFile in createdFiles)
                {
                    builder.AppendLine("* " + createdFile);
                }
            }

            if (updatedFiles.Length > 0)
            {
                builder.AppendLine("Updated files:");
                foreach (string updatedFile in updatedFiles)
                {
                    builder.AppendLine("* " + updatedFile);
                }
            }

            if (deletedFiles.Length > 0)
            {
                builder.AppendLine("Deleted files:");
                foreach (string deletedFile in deletedFiles)
                {
                    builder.AppendLine("* " + deletedFile);
                }
            }

            return builder.ToString();
        }
    }
}
