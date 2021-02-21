using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.KVConfiguration.Core;
using JetBrains.Annotations;
using Marten;
using MediatR;
using Milou.Deployer.Core;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Agents.Pools;
using Milou.Deployer.Web.Core.Caching;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Environments;
using Milou.Deployer.Web.Core.Deployment.Messages;
using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.Core.Deployment.Targets;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Milou.Deployer.Web.Marten.Agents;
using Milou.Deployer.Web.Marten.DeploymentTasks;
using Milou.Deployer.Web.Marten.EnvironmentTypes;
using Milou.Deployer.Web.Marten.Settings;
using Milou.Deployer.Web.Marten.Targets;
using Newtonsoft.Json;
using Serilog;

namespace Milou.Deployer.Web.Marten
{
    [UsedImplicitly]
    public class MartenStore : IDeploymentTargetReadService,
        IRequestHandler<CreateOrganization, CreateOrganizationResult>,
        IRequestHandler<CreateProject, CreateProjectResult>,
        IRequestHandler<CreateTarget, CreateTargetResult>,
        IRequestHandler<UpdateDeploymentTarget, UpdateDeploymentTargetResult>,
        IRequestHandler<DeploymentHistoryRequest, DeploymentHistoryResponse>,
        IRequestHandler<DeploymentLogRequest, DeploymentLogResponse>,
        IRequestHandler<RemoveTarget, Unit>,
        IRequestHandler<EnableTarget, Unit>,
        IRequestHandler<DisableTarget, Unit>,
        IRequestHandler<CreateEnvironment, CreateEnvironmentResult>,
        IRequestHandler<CreateDeploymentTaskPackage, Unit>,
        IRequestHandler<GetAgentPoolsQuery, AgentPoolListResult>,
        IRequestHandler<CreateAgentPool, CreateAgentPoolResult>,
        IRequestHandler<AssignTargetToPool, AssignTargetToPoolResult>,
        IRequestHandler<GetAgentRequest, AgentInfo?>,
        IRequestHandler<AssignAgentToPool, AssignAgentToPoolResult>,
        IRequestHandler<GetAgentsInPoolQuery, AgentsInPoolResult>,
        IRequestHandler<GetAgentsQuery, AgentsQueryResult>,
        IRequestHandler<ResetAgentToken, ResetAgentTokenResult>
    {
        private readonly ICustomMemoryCache _cache;
        private readonly IDocumentStore _documentStore;
        private readonly ILogger _logger;

        private readonly IMediator _mediator;
        private static readonly AgentsInPoolResult EmptyResult = new(ImmutableArray<AgentId>.Empty);

        public MartenStore([NotNull] IDocumentStore documentStore,
            ILogger logger,
            IMediator mediator,
            ICustomMemoryCache cache)
        {
            _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
            _logger = logger;
            _mediator = mediator;
            _cache = cache;
        }

        public Task<DeploymentTarget?> GetDeploymentTargetAsync(
            string deploymentTargetId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deploymentTargetId))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(deploymentTargetId));
            }

            return FindDeploymentTargetAsync(deploymentTargetId, cancellationToken);
        }

        public async Task<ImmutableArray<OrganizationInfo>> GetOrganizationsAsync(
            CancellationToken cancellationToken = default)
        {
            using IQuerySession session = _documentStore.QuerySession();
            try
            {
                IReadOnlyList<DeploymentTargetData> targets =
                    await session.Query<DeploymentTargetData>()
                        .Where(target => target.Enabled)
                        .ToListAsync(cancellationToken);

                IReadOnlyList<ProjectData> projects =
                    await session.Query<ProjectData>()
                        .ToListAsync<ProjectData>(cancellationToken);

                IReadOnlyList<OrganizationData> organizations =
                    await session.Query<OrganizationData>()
                        .ToListAsync<OrganizationData>(
                            cancellationToken);

                var environmentTypes = await _documentStore.GetEnvironmentTypes(_cache, cancellationToken);

                var organizationsInfo =
                    MapDataToOrganizations(organizations, projects, targets, environmentTypes);

                return organizationsInfo;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Warning(ex, "Could not get any organizations targets");
                return ImmutableArray<OrganizationInfo>.Empty;
            }
        }

        public async Task<ImmutableArray<DeploymentTarget>> GetDeploymentTargetsAsync(TargetOptions? options = default,
            CancellationToken stoppingToken = default)
        {
            using IQuerySession session = _documentStore.QuerySession();

            bool Filter(DeploymentTarget target)
            {
                if (options?.OnlyEnabled != false)
                {
                    return target.Enabled;
                }

                return true;
            }

            try
            {
                var environmentTypes = await _documentStore.GetEnvironmentTypes(_cache, stoppingToken);

                IReadOnlyList<DeploymentTargetData> targets = await session.Query<DeploymentTargetData>()
                    .ToListAsync<DeploymentTargetData>(stoppingToken);

                var deploymentTargets = targets
                    .Select(targetData => MapDataToTarget(targetData, environmentTypes)!)
                    .Where(Filter)
                    .OrderBy(target => target.Name)
                    .ToImmutableArray();

                return deploymentTargets;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Warning(ex, "Could not get any deployment targets");
                return ImmutableArray<DeploymentTarget>.Empty;
            }
        }

        public async Task<ImmutableArray<ProjectInfo>> GetProjectsAsync(
            string organizationId,
            CancellationToken cancellationToken = default)
        {
            using IQuerySession session = _documentStore.QuerySession();
            IReadOnlyList<ProjectData> projects =
                await session.Query<ProjectData>().Where(project =>
                        project.OrganizationId.Equals(organizationId, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync(cancellationToken);

            return projects.Select(project =>
                    new ProjectInfo(project.OrganizationId, project.Id, ImmutableArray<DeploymentTarget>.Empty))
                .ToImmutableArray();
        }

        public async Task<AssignAgentToPoolResult> Handle(AssignAgentToPool request,
            CancellationToken cancellationToken)
        {
            using var session = _documentStore.OpenSession();
            var agentData = await session.LoadAsync<AgentPoolAssignmentData>(DocumentConstants.AgentAssignmentsId, cancellationToken);

            agentData ??= new AgentPoolAssignmentData {Id = DocumentConstants.AgentAssignmentsId};

            if (!agentData.Agents.ContainsKey(request.AgentId.Value))
            {
                agentData.Agents.Add(request.AgentId.Value, "");
            }

            if (agentData.Agents[request.AgentId.Value] == request.AgentPoolId.Value)
            {
                return new AssignAgentToPoolResult
                {
                    AgentId = request.AgentId,
                    AgentPoolId = request.AgentPoolId,
                    Updated = false
                };
            }

            agentData.Agents[request.AgentId.Value] = request.AgentPoolId.Value;

            session.Store(agentData);

            await session.SaveChangesAsync(cancellationToken);

            return new AssignAgentToPoolResult
            {
                AgentId = request.AgentId,
                AgentPoolId = request.AgentPoolId,
                Updated = false
            };
        }

        public async Task<AssignTargetToPoolResult> Handle(AssignTargetToPool request,
            CancellationToken cancellationToken)
        {
            using var documentSession = _documentStore.OpenSession();
            string id = $"/poolAssignment/-{request.DeploymentTargetId}";
            var assignment = await documentSession.LoadAsync<AgentPoolTargetAssignmentData>(id, cancellationToken);

            assignment ??= new AgentPoolTargetAssignmentData {Id = id};

            assignment.PoolId = request.AgentPoolId;

            documentSession.Store(assignment);

            await documentSession.SaveChangesAsync(cancellationToken);

            return new AssignTargetToPoolResult();
        }

        public async Task<CreateAgentPoolResult> Handle(CreateAgentPool request, CancellationToken cancellationToken)
        {
            using IDocumentSession session = _documentStore.OpenSession();

            session.Store(new AgentPoolData {Id = request.AgentPoolId.Value, Name = request.Name.Value});

            await session.SaveChangesAsync(cancellationToken);

            return new CreateAgentPoolResult();
        }

        public async Task<Unit> Handle(CreateDeploymentTaskPackage request, CancellationToken cancellationToken)
        {
            using IDocumentSession session = _documentStore.OpenSession();

            var deploymentTaskPackageData = new DeploymentTaskPackageData
            {
                Id = request.DeploymentTaskPackage.DeploymentTaskId,
                DeploymentTargetId = request.DeploymentTaskPackage.DeploymentTargetId,
                NuGetConfigXml = request.DeploymentTaskPackage.NugetConfigXml,
                ManifestJson = request.DeploymentTaskPackage.ManifestJson,
                PublishSettingsXml = request.DeploymentTaskPackage.PublishSettingsXml,
                AgentId = request.DeploymentTaskPackage.AgentId,
                ProcessArgs = request.DeploymentTaskPackage.DeployerProcessArgs.ToArray()
            };

            session.Store(deploymentTaskPackageData);

            await session.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        public async Task<CreateEnvironmentResult> Handle([NotNull] CreateEnvironment request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using IDocumentSession session = _documentStore.OpenSession();
            EnvironmentTypeData environmentTypeData =
                await session.LoadAsync<EnvironmentTypeData>(request.EnvironmentTypeId.Trim(), cancellationToken);

            if (environmentTypeData is { })
            {
                return new CreateEnvironmentResult(environmentTypeData.Id, Result.AlreadyExists);
            }

            EnvironmentTypeData data = await session.StoreEnvironmentType(request, _cache, _logger, cancellationToken);

            if (string.IsNullOrWhiteSpace(data.Id))
            {
                return new CreateEnvironmentResult("", Result.Failed);
            }

            return new CreateEnvironmentResult(data.Id, Result.Created);
        }

        public async Task<CreateOrganizationResult> Handle(
            [NotNull] CreateOrganization request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            CreateOrganizationResult result = await CreateOrganizationAsync(request, cancellationToken);

            return result;
        }

        public Task<CreateProjectResult> Handle([NotNull] CreateProject request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return CreateProjectAsync(request, cancellationToken);
        }

        public async Task<CreateTargetResult> Handle([NotNull] CreateTarget createTarget,
            CancellationToken cancellationToken)
        {
            if (createTarget is null)
            {
                throw new ArgumentNullException(nameof(createTarget));
            }

            if (!createTarget.IsValid)
            {
                return new CreateTargetResult(new ValidationError("Invalid"));
            }

            using (IDocumentSession session = _documentStore.OpenSession())
            {
                var data = new DeploymentTargetData {Id = createTarget.Id, Name = createTarget.Name};

                session.Store(data);

                await session.SaveChangesAsync(cancellationToken);
            }

            _logger.Information("Created target with id {Id}", createTarget.Id);

            return new CreateTargetResult(createTarget.Id, createTarget.Name);
        }

        public async Task<DeploymentHistoryResponse> Handle(
            DeploymentHistoryRequest request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<TaskMetadata> taskMetadata;
            using (IDocumentSession session = _documentStore.LightweightSession())
            {
                taskMetadata = await session.Query<TaskMetadata>()
                    .Where(item =>
                        item.DeploymentTargetId.Equals(request.DeploymentTargetId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.FinishedAtUtc)
                    .ToListAsync(cancellationToken);
            }

            return new DeploymentHistoryResponse(taskMetadata
                .Select(item =>
                    new DeploymentTaskInfo(
                        item.DeploymentTaskId,
                        item.Metadata,
                        item.StartedAtUtc,
                        item.FinishedAtUtc,
                        item.ExitCode,
                        item.PackageId,
                        item.Version))
                .ToImmutableArray());
        }

        public async Task<DeploymentLogResponse> Handle(
            DeploymentLogRequest request,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<LogItem> taskLog;

            string id = $"deploymentTaskLog/{request.DeploymentTaskId}";

            int level = (int)request.Level;

            using (IDocumentSession session = _documentStore.LightweightSession())
            {
                taskLog = await session.Query<LogItem>()
                    .Where(log => log.TaskLogId == id && log.Level >= level)
                    .ToListAsync(cancellationToken);
            }

            if (taskLog is null)
            {
                return new DeploymentLogResponse(Array.Empty<LogItem>());
            }

            return new DeploymentLogResponse(taskLog);
        }

        public async Task<Unit> Handle([NotNull] DisableTarget request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using IDocumentSession session = _documentStore.OpenSession();

            DeploymentTargetData deploymentTargetData =
                await session.LoadAsync<DeploymentTargetData>(request.TargetId, cancellationToken);

            if (deploymentTargetData is null)
            {
                return Unit.Value;
            }

            deploymentTargetData.Enabled = false;

            session.Store(deploymentTargetData);

            await session.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        public async Task<Unit> Handle(EnableTarget request, CancellationToken cancellationToken)
        {
            using IDocumentSession session = _documentStore.OpenSession();

            DeploymentTargetData deploymentTargetData =
                await session.LoadAsync<DeploymentTargetData>(request.TargetId, cancellationToken);

            if (deploymentTargetData is null)
            {
                return Unit.Value;
            }

            deploymentTargetData.Enabled = true;

            session.Store(deploymentTargetData);

            await session.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        public async Task<AgentPoolListResult> Handle(GetAgentPoolsQuery request, CancellationToken cancellationToken)
        {
            using IDocumentSession session = _documentStore.OpenSession();
            var items =
                await session.Query<AgentPoolData>().ToListAsync(cancellationToken);

            return new AgentPoolListResult(items.Select(MapAgentPool).ToImmutableArray());
        }

        public async Task<AgentInfo?> Handle(GetAgentRequest request, CancellationToken cancellationToken)
        {
            using var documentSession = _documentStore.QuerySession();

            var agentData = await documentSession.LoadAsync<AgentData>(request.AgentId.Value, cancellationToken);

            if (agentData is null)
            {
                return null;
            }

            return MapAgentData(agentData);
        }
        public async Task<AgentsQueryResult> Handle(GetAgentsQuery request, CancellationToken cancellationToken)
        {
            using var documentSession = _documentStore.QuerySession();

            var agentsData = await documentSession.Query<AgentData>().ToListAsync<AgentData>(cancellationToken);

            if (agentsData.Count == 0)
            {
                return new AgentsQueryResult(ImmutableArray<AgentInfo>.Empty);
            }

            return new AgentsQueryResult(agentsData.Select(MapAgentData).NotNull().ToImmutableArray());
        }

        public async Task<AgentsInPoolResult> Handle(GetAgentsInPoolQuery request, CancellationToken cancellationToken)
        {
            using var session = _documentStore.OpenSession();

            var agentData = await session.LoadAsync<AgentPoolAssignmentData>(DocumentConstants.AgentAssignmentsId, cancellationToken);

            if (agentData is null)
            {
                return EmptyResult;
            }

            var agentIds = agentData.Agents
                .Where(pair => pair.Value == request.AgentPoolId.Value)
                .Select(pair => new AgentId(pair.Key))
                .ToImmutableArray();

            return new AgentsInPoolResult(agentIds);
        }

        public async Task<Unit> Handle([NotNull] RemoveTarget request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.DeploymentTargetId is null)
            {
                throw new InvalidOperationException($"{nameof(request.DeploymentTargetId)} is required");
            }

            using (IDocumentSession session = _documentStore.OpenSession())
            {
                IReadOnlyList<DeploymentTargetData> deploymentTargetData = await session.Query<DeploymentTargetData>()
                    .Where(target => target.Id == request.DeploymentTargetId).ToListAsync(cancellationToken);

                if (deploymentTargetData.Count == 0)
                {
                    _logger.Warning("Could not delete deployment target with id {DeploymentTargetId}, not found",
                        request.DeploymentTargetId);

                    return Unit.Value;
                }

                session.Delete<DeploymentTargetData>(request.DeploymentTargetId);
                session.DeleteWhere<TaskMetadata>(m =>
                    m.DeploymentTargetId.Equals(request.DeploymentTargetId, StringComparison.OrdinalIgnoreCase));

                await session.SaveChangesAsync(cancellationToken);
            }

            _logger.Information("Deleted deployment target with id {DeploymentTargetId}", request.DeploymentTargetId);

            return Unit.Value;
        }

        public async Task<UpdateDeploymentTargetResult> Handle(
            [NotNull] UpdateDeploymentTarget request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string targetName;

            string id;

            using (IDocumentSession session = _documentStore.OpenSession())
            {
                DeploymentTargetData data =
                    await session.LoadAsync<DeploymentTargetData>(request.Id, cancellationToken);

                if (data is null)
                {
                    return new UpdateDeploymentTargetResult("", "", new ValidationError("Not found"));
                }

                id = data.Id;
                targetName = data.Name;

                data.NuGetData ??= new NuGetData();
                data.PackageId = request.PackageId;
                data.Url = request.Url;
                data.IisSiteName = request.IisSiteName;
                data.AllowExplicitPreRelease = request.AllowExplicitPreRelease;
                data.AutoDeployEnabled = request.AutoDeployEnabled;
                data.PublishSettingsXml = request.PublishSettingsXml;
                data.TargetDirectory = request.TargetDirectory;
                data.WebConfigTransform = request.WebConfigTransform;
                data.ExcludedFilePatterns = request.ExcludedFilePatterns;
                data.FtpPath = request.FtpPath?.Path;
                data.PublishType = request.PublishType.Name;
                data.EnvironmentTypeId = request.EnvironmentTypeId;
                data.NuGetData.NuGetConfigFile = request.NugetConfigFile;
                data.NuGetData.NuGetPackageSource = request.NugetPackageSource;
                data.NuGetData.PackageListTimeout = request.PackageListTimeout;
                data.MetadataTimeout = request.MetadataTimeout;
                data.RequireEnvironmentConfig = request.RequireEnvironmentConfig;
                data.EnvironmentConfiguration = request.EnvironmentConfiguration;
                data.PackageListPrefixEnabled = request.PackageListPrefixEnabled;
                data.PackageListPrefix = request.PackageListPrefix;
                session.Store(data);

                await session.SaveChangesAsync(cancellationToken);
            }

            _logger.Information("Updated target with id {Id}", request.Id);

            var updateDeploymentTargetResult = new UpdateDeploymentTargetResult(targetName, id);

            await _mediator.Publish(updateDeploymentTargetResult, cancellationToken);

            return updateDeploymentTargetResult;
        }

        public async Task Handle(DeploymentTaskCreated notification, CancellationToken cancellationToken)
        {
            using IDocumentSession session = _documentStore.OpenSession();
            session.Store(new DeploymentTaskData
            {
                Id = notification.DeploymentTask.DeploymentTaskId,
                PackageVersion = notification.DeploymentTask.PackageVersion,
                DeploymentTargetId = notification.DeploymentTask.DeploymentTargetId,
                StartedBy = notification.DeploymentTask.StartedBy,
                AgentId = ""
            });

            await session.SaveChangesAsync(cancellationToken);
        }

        private async Task<CreateOrganizationResult> CreateOrganizationAsync(
            CreateOrganization createOrganization,
            CancellationToken cancellationToken)
        {
            if (!createOrganization.IsValid)
            {
                return new CreateOrganizationResult(new ValidationError("Missing ID"));
            }

            using (IDocumentSession session = _documentStore.OpenSession())
            {
                var data = new OrganizationData {Id = createOrganization.Id};

                session.Store(data);

                await session.SaveChangesAsync(cancellationToken);
            }

            _logger.Information("Created organization with id {Id}", createOrganization.Id);

            return new CreateOrganizationResult();
        }

        private async Task<CreateProjectResult> CreateProjectAsync(
            CreateProject createProject,
            CancellationToken cancellationToken)
        {
            if (!createProject.IsValid)
            {
                return new CreateProjectResult(new ValidationError("Id or organization id is invalid"));
            }

            using (IDocumentSession session = _documentStore.OpenSession())
            {
                var data = new ProjectData {Id = createProject.Id, OrganizationId = createProject.OrganizationId};

                session.Store(data);

                await session.SaveChangesAsync(cancellationToken);
            }

            _logger.Information("Created project with id {Id}", createProject.Id);

            return new CreateProjectResult(createProject.Id);
        }

        private async Task<DeploymentTarget?> FindDeploymentTargetAsync(string deploymentTargetId,
            CancellationToken cancellationToken)
        {
            using IQuerySession session = _documentStore.QuerySession();
            try
            {
                DeploymentTargetData deploymentTargetData = await session.Query<DeploymentTargetData>()
                    .SingleOrDefaultAsync(target =>
                            target.Id.Equals(deploymentTargetId, StringComparison.OrdinalIgnoreCase),
                        cancellationToken);

                var environmentTypes = await _documentStore.GetEnvironmentTypes(_cache, cancellationToken);
                var deploymentTarget = MapDataToTarget(deploymentTargetData, environmentTypes);

                return deploymentTarget ?? DeploymentTarget.None;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Warning(ex, "Could not get deployment target with id {Id}", deploymentTargetId);
                return DeploymentTarget.None;
            }
        }

        private AgentInfo MapAgentData(AgentData agent) => new(new AgentId(agent.AgentId));

        private AgentPoolInfo MapAgentPool(AgentPoolData agentPoolData) => new(new AgentPoolId(agentPoolData.Id), new (agentPoolData.Name ?? "N/A"));

        private ImmutableArray<OrganizationInfo> MapDataToOrganizations(
            IReadOnlyList<OrganizationData> organizations,
            IReadOnlyList<ProjectData> projects,
            IReadOnlyList<DeploymentTargetData> targets,
            ImmutableArray<EnvironmentType> environmentTypes) =>
            organizations.Select(org => new OrganizationInfo(org.Id,
                    projects
                        .Where(project => project.OrganizationId.Equals(org.Id, StringComparison.OrdinalIgnoreCase))
                        .Select(project =>
                        {
                            IEnumerable<DeploymentTargetData> deploymentTargetItems = targets
                                .Where(target =>
                                    target.ProjectId is {}
                                    && target.ProjectId.Equals(project.Id, StringComparison.OrdinalIgnoreCase));

                            return new ProjectInfo(org.Id,
                                project.Id,
                                deploymentTargetItems
                                    .Select(s => MapDataToTarget(s, environmentTypes))
                                    .Where(item => item is {})!
                            );
                        })
                        .ToImmutableArray()))
                .Concat(new[]
                {
                    new OrganizationInfo("NA",
                        new[]
                        {
                            new ProjectInfo(
                                "NA",
                                "NA",
                                targets
                                    .NotNull()
                                    .Select(s => MapDataToTarget(s, environmentTypes))
                                    .NotNull())
                        })
                })
                .ToImmutableArray();

        private DeploymentTarget? MapDataToTarget(DeploymentTargetData? deploymentTargetData,
            ImmutableArray<EnvironmentType> environmentTypes)
        {
            if (deploymentTargetData is null)
            {
                return null;
            }

            EnvironmentType environmentType =
                environmentTypes.SingleOrDefault(type =>
                    type.Id.Equals(deploymentTargetData.EnvironmentTypeId, StringComparison.Ordinal)) ??
                EnvironmentType.Unknown;

            DeploymentTarget? deploymentTargetAsync = null;
            try
            {
                deploymentTargetAsync = new DeploymentTarget(
                    deploymentTargetData.Id,
                    deploymentTargetData.Name,
                    deploymentTargetData.PackageId.WithDefault(Constants.NotAvailable)!,
                    deploymentTargetData.PublishSettingsXml,
                    deploymentTargetData.AllowExplicitPreRelease,
                    deploymentTargetData.Url,
                    iisSiteName: deploymentTargetData.IisSiteName,
                    autoDeployEnabled: deploymentTargetData.AutoDeployEnabled,
                    targetDirectory: deploymentTargetData.TargetDirectory,
                    webConfigTransform: deploymentTargetData.WebConfigTransform,
                    excludedFilePatterns: deploymentTargetData.ExcludedFilePatterns,
                    environmentTypeId: deploymentTargetData.EnvironmentTypeId,
                    environmentType: environmentType,
                    enabled: deploymentTargetData.Enabled,
                    publishType: deploymentTargetData.PublishType,
                    ftpPath: deploymentTargetData.FtpPath,
                    metadataTimeout: deploymentTargetData.MetadataTimeout,
                    nuget: MapNuGet(deploymentTargetData.NuGetData),
                    requireEnvironmentConfig: deploymentTargetData.RequireEnvironmentConfig,
                    environmentConfiguration: deploymentTargetData.EnvironmentConfiguration,
                    packageListPrefixEnabled: deploymentTargetData.PackageListPrefixEnabled,
                    packageListPrefix: deploymentTargetData.PackageListPrefix);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not get deployment target from data {Data}",
                    JsonConvert.SerializeObject(deploymentTargetData));
            }

            return deploymentTargetAsync;
        }

        private TargetNuGetSettings MapNuGet(NuGetData? nugetData)
        {
            if (nugetData is null)
            {
                return new TargetNuGetSettings();
            }

            return new TargetNuGetSettings
            {
                PackageListTimeout = nugetData.PackageListTimeout,
                NuGetPackageSource = nugetData.NuGetPackageSource,
                NuGetConfigFile = nugetData.NuGetConfigFile
            };
        }

        public async Task<ResetAgentTokenResult> Handle(ResetAgentToken request, CancellationToken cancellationToken)
        {
            using var session = _documentStore.OpenSession();

            var agentData = await session.LoadAsync<AgentData>(request.AgentId.Value, cancellationToken);

            if (agentData is null)
            {
                return new ResetAgentTokenResult("");
            }

            var agentInstallConfiguration =
                await _mediator.Send(new CreateAgentInstallConfiguration(request.AgentId), cancellationToken);

            return new ResetAgentTokenResult(agentInstallConfiguration.AccessToken);
        }
    }
}