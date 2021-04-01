﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using JetBrains.Annotations;
using Milou.Deployer.Web.Core;

namespace Milou.Deployer.Web.Tests.Integration
{
    internal static class TcpHelper
    {
        private static readonly ConcurrentDictionary<int, PortPoolRental> Rentals =
            new();

        private static void Return([NotNull] PortPoolRental rental)
        {
            if (rental is null)
            {
                throw new ArgumentNullException(nameof(rental));
            }

            Rentals.TryRemove(rental.Port, out _);
        }

        public static PortPoolRental GetAvailablePort(in PortPoolRange range, IEnumerable<int>? excludes = null)
        {
            var excluded = (excludes ?? new List<int>()).ToList();

            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] activeTcpConnections = ipGlobalProperties.GetActiveTcpConnections();

            var random = new Random();
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int port = random.Next(range.StartPort, range.EndPort);

                bool portIsInUse = activeTcpConnections.Any(tcpPort => tcpPort.LocalEndPoint.Port == port);

                if (!Rentals.ContainsKey(port) && !portIsInUse && !excluded.Any(excludedPort => excludedPort == port))
                {
                    var portPoolRental = new PortPoolRental(port, Return);

                    if (Rentals.TryAdd(port, portPoolRental))
                    {
                        return portPoolRental;
                    }
                }
            }

            throw new DeployerAppException($"Could not find any TCP port in range {range.Format()}");
        }
    }
}