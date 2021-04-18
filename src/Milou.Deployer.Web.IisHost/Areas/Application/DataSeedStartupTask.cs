﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Time;
using Arbor.KVConfiguration.Core;
using JetBrains.Annotations;
using Marten;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Core.Configuration;
using Milou.Deployer.Web.Core.Deployment.Targets;
using Milou.Deployer.Web.Core.Startup;
using Milou.Deployer.Web.Marten.Targets;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Application
{
    [UsedImplicitly]
    public class DataSeedStartupTask : BackgroundService, IStartupTask
    {
        private readonly IKeyValueConfiguration _configuration;
        private readonly ImmutableArray<IDataSeeder> _dataSeeders;
        private readonly ILogger _logger;
        private readonly IDocumentStore? _store;
        private readonly TimeoutHelper _timeoutHelper;

        public DataSeedStartupTask(
            IEnumerable<IDataSeeder> dataSeeders,
            IKeyValueConfiguration configuration,
            ILogger logger,
            TimeoutHelper timeoutHelper,
            IDocumentStore? store)
        {
            _dataSeeders = dataSeeders.SafeToImmutableArray();
            _configuration = configuration;
            _logger = logger;
            _timeoutHelper = timeoutHelper;
            _store = store;
        }

        public bool IsCompleted { get; private set; }

        protected override async Task ExecuteAsync(CancellationToken startupCancellationToken)
        {
            await Task.Yield();

            if (_dataSeeders.Length > 0)
            {
                _logger.Debug("Running data seeders");

                await Task.Run(() => RunSeeders(startupCancellationToken), startupCancellationToken);
            }
            else
            {
                _logger.Debug("No data seeders were found");
                IsCompleted = true;
            }
        }

        private async Task RunSeeders(CancellationToken cancellationToken)
        {
            if (_store is { })
            {
                bool retry = true;

                while (retry && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await TryReadFromDatabase(cancellationToken);

                        retry = false;
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        var messages = new List<string> {"the database system is starting up", "57P03", "0x80004005"};

                        if (messages.Any(message => ex.Message.Contains(message, StringComparison.OrdinalIgnoreCase)))
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
                            _logger.Debug("Database is not ready");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            _logger.Debug("Database is ready");

            if (!int.TryParse(_configuration[DeployerAppConstants.SeedTimeoutInSeconds],
                    out int seedTimeoutInSeconds) ||
                seedTimeoutInSeconds <= 0)
            {
                seedTimeoutInSeconds = 20;
            }

            if (bool.TryParse(_configuration[DeployerAppConstants.SeedEnabled],
                out bool seedEnabled) && !seedEnabled)
            {
                _logger.Information("Seeders disabled");
                IsCompleted = true;
                return;
            }

            _logger.Debug("Found {SeederCount} data seeders", _dataSeeders.Length);

            foreach (IDataSeeder dataSeeder in _dataSeeders.OrderBy(seeder => seeder.Order))
            {
                try
                {
                    using CancellationTokenSource startupToken =
                        _timeoutHelper.CreateCancellationTokenSource(TimeSpan.FromSeconds(seedTimeoutInSeconds));
                    using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        startupToken.Token);
                    _logger.Debug("Running data seeder {Seeder}", dataSeeder.GetType().FullName);
                    await dataSeeder.SeedAsync(linkedToken.Token);
                }
                catch (TaskCanceledException ex)
                {
                    _logger.Error(ex, "Could not run seeder {Seeder}, timeout {Timeout} seconds expired",
                        dataSeeder.GetType().Name, seedTimeoutInSeconds);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to run seeder {Seeder}", dataSeeder.GetType().Name);
                }
            }

            IsCompleted = true;

            _logger.Debug("Done running data seeders");
        }

        private async Task TryReadFromDatabase(CancellationToken cancellationToken)
        {
            if (_store is null)
            {
                return;
            }

            using var session = _store.OpenSession();

            _ = await session.Query<DeploymentTargetData>().ToListAsync(cancellationToken);
        }
    }
}