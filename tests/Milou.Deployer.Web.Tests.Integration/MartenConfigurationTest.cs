﻿using Milou.Deployer.Web.Marten;
using Milou.Deployer.Web.Marten.Abstractions;
using Xunit;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class MartenConfigurationTest
    {
        [Fact]
        public void ShouldMakeConnectionStringAnonymous()
        {
            var martenConfiguration = new MartenConfiguration(
                "Server=localhost;Port=1000;User Id=testUser;Password=p@ssword;Database=postgres;Pooling=false",
                true);

            string asString = martenConfiguration.ToString();

            Assert.Equal(
                "ConnectionString: [Server=localhost; Port=1000; User Id=*****; Password=*****; Database=postgres; Pooling=false], Enabled: true",
                asString);
        }
    }
}