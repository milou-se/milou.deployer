using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Microsoft.Extensions.Primitives;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Targets;

namespace Milou.Deployer.Web.Tests.Integration
{
    public static class TestDataCreator
    {
        public const string Testtarget = "TestTarget";

        public static Task<IReadOnlyCollection<OrganizationInfo>> CreateData()
        {
            var targets = new List<OrganizationInfo>
            {
                new("testorg",
                    new List<ProjectInfo>
                    {
                        new("testorg",
                            "testproject",
                            new List<DeploymentTarget>
                            {
                                new(new DeploymentTargetId(Testtarget),
                                    "Test target",
                                    "MilouDeployerWebTest",
                                    allowExplicitPreRelease: false,
                                    autoDeployEnabled: true,
                                    targetDirectory: Environment.GetEnvironmentVariable("TestDeploymentTargetPath"),
                                    url: Environment.GetEnvironmentVariable("TestDeploymentUri").ParseUriOrDefault(),
                                    emailNotificationAddresses: new StringValues("noreply@localhost.local"),
                                    enabled: true)
                            })
                    })
            };

            return Task.FromResult<IReadOnlyCollection<OrganizationInfo>>(targets);
        }
    }
}