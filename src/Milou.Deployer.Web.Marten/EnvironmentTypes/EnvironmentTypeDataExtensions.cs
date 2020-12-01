using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Marten;
using Milou.Deployer.Web.Core.Caching;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Environments;
using Serilog;

namespace Milou.Deployer.Web.Marten.EnvironmentTypes
{
    internal static class EnvironmentTypeDataExtensions
    {
        private const string CacheKey = "urn:milou:deployer:web:cache:environment-types";

        public static async Task<ImmutableArray<EnvironmentType>> GetEnvironmentTypes(this IDocumentStore documentStore,
            ICustomMemoryCache memoryCache,
            CancellationToken cancellationToken =
                default)
        {
            if (memoryCache.TryGetValue(CacheKey, out EnvironmentType[]? environmentTypes))
            {
                return environmentTypes.SafeToImmutableArray();
            }

            using IQuerySession querySession = documentStore.QuerySession();

            IReadOnlyList<EnvironmentTypeData> environmentTypeData = await querySession.Query<EnvironmentTypeData>()
                .ToListAsync<EnvironmentTypeData>(cancellationToken);

            EnvironmentType[] enumerable = environmentTypeData.Select(EnvironmentTypeData.MapFromData).ToArray();

            memoryCache.SetValue(CacheKey, enumerable);

            return enumerable.ToImmutableArray();
        }

        public static async Task<EnvironmentTypeData> StoreEnvironmentType(this IDocumentSession session,
            CreateEnvironment request,
            ICustomMemoryCache memoryCache,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            memoryCache.Invalidate(CacheKey);

            try
            {
                var environmentTypeData = EnvironmentTypeData.MapToData(new EnvironmentType(
                    request.EnvironmentTypeId.Trim(),
                    request.EnvironmentTypeName.Trim(), PreReleaseBehavior.Parse(request.PreReleaseBehavior)));

                session.Store(environmentTypeData);

                await session.SaveChangesAsync(cancellationToken);

                return environmentTypeData;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error(ex, "Could not save environment type {Request}", request);
                return EnvironmentTypeData.Empty;
            }
        }
    }
}