﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Messages;

namespace Milou.Deployer.Web.IisHost.Areas.Deployment.Signaling
{
    [UsedImplicitly]
    public class DeploymentLogSubscriptionHandler :
        IRequestHandler<SubscribeToDeploymentLog>,
        IRequestHandler<UnsubscribeToDeploymentLog>
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> TargetMapping = new(StringComparer.OrdinalIgnoreCase);

        public Task<Unit> Handle(SubscribeToDeploymentLog request, CancellationToken cancellationToken)
        {
            if (TargetMapping.TryGetValue(request.DeploymentTargetId, out var subscribers))
            {
                subscribers.Add(request.ConnectionId);
            }
            else
            {
                TargetMapping.TryAdd(
                    request.DeploymentTargetId,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) {request.ConnectionId});
            }

            return Task.FromResult(Unit.Value);
        }

        public Task<Unit> Handle(UnsubscribeToDeploymentLog request, CancellationToken cancellationToken)
        {
            HashSet<string>[] hashSets = TargetMapping
                .Where(pair => pair.Value.Contains(request.ConnectionId))
                .Select(pair => pair.Value)
                .ToArray();

            foreach (HashSet<string> hashSet in hashSets)
            {
                hashSet.Remove(request.ConnectionId);
            }

            return Task.FromResult(Unit.Value);
        }

        public static ImmutableHashSet<string> TryGetTargetSubscribers([NotNull] string deploymentTargetId)
        {
            if (string.IsNullOrWhiteSpace(deploymentTargetId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(deploymentTargetId));
            }

            bool tryGetTargetSubscribers =
                TargetMapping.TryGetValue(deploymentTargetId, out var subscribers);

            if (!tryGetTargetSubscribers)
            {
                return ImmutableHashSet<string>.Empty;
            }

            return subscribers.SafeToImmutableArray().ToImmutableHashSet(StringComparer.Ordinal);
        }
    }
}