﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Core.Deployment.Messages;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Milou.Deployer.Web.Core.Email;
using MimeKit;

namespace Milou.Deployer.Web.IisHost.Areas.Email
{
    [UsedImplicitly]
    public class DeploymentFinishedEmailHandler : INotificationHandler<DeploymentFinished>
    {
        private readonly EmailNotificationConfiguration _emailNotificationConfiguration;
        private readonly ISmtpService _smtpService;

        public DeploymentFinishedEmailHandler(
            [NotNull] ISmtpService smtpService,
            [NotNull] EmailNotificationConfiguration emailNotificationConfiguration)
        {
            _smtpService = smtpService ?? throw new ArgumentNullException(nameof(smtpService));
            _emailNotificationConfiguration = emailNotificationConfiguration ??
                                              throw new ArgumentNullException(nameof(emailNotificationConfiguration));
        }

        public Task Handle(DeploymentFinished notification, CancellationToken cancellationToken)
        {
            if (!_emailNotificationConfiguration.Enabled)
            {
                return Task.CompletedTask;
            }

            if (!_emailNotificationConfiguration.IsValid)
            {
                return Task.CompletedTask;
            }

            string result = notification.DeploymentTask.Status == WorkTaskStatus.Done ? "succeeded" : "failed";

            string subject =
                $"Deployment of {notification.DeploymentTask.PackageId} {notification.DeploymentTask.SemanticVersion.ToNormalizedString()} to {notification.DeploymentTask.DeploymentTargetId} {result}";

            string body = $@"{notification.DeploymentTask.DeploymentTargetId}
Status: {notification.DeploymentTask.Status}
Finished at time (UTC): {notification.FinishedAtUtc:O}
Package ID: {notification.DeploymentTask.PackageId}
Deployment task ID: {notification.DeploymentTask.DeploymentTaskId}
Version: {notification.DeploymentTask.SemanticVersion.ToNormalizedString()}
Log: {string.Join(Environment.NewLine, notification.LogLines.Select(line => line.Message))}
";

            var message = new MimeMessage {Body = new TextPart("plain") {Text = body}, Subject = subject};

            foreach (EmailAddress email in _emailNotificationConfiguration.To)
            {
                message.To.Add(MailboxAddress.Parse(email.Address));
            }

            if (_emailNotificationConfiguration.From is {})
            {
                message.From.Add(MailboxAddress.Parse(_emailNotificationConfiguration.From.Address));
            }

            return _smtpService.SendAsync(message, cancellationToken);
        }
    }
}