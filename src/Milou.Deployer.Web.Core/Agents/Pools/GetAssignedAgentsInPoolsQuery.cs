﻿using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public sealed record GetAssignedAgentsInPoolsQuery
        : IQuery<AssignedAgentsInPoolsQueryResult>;
}