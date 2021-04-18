﻿using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.ExtensionMethods;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class AllControllers
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public AllControllers(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [PublicAPI]
        public static IEnumerable<object[]> Data
        {
            get
            {
                string[] assemblyNameStartsWith = {"Milou"};
                var filteredAssemblies = ApplicationAssemblies.FilteredAssemblies(assemblyNameStartsWith: assemblyNameStartsWith,
useCache: false);

                return filteredAssemblies
                    .SelectMany(assembly => assembly.GetLoadableTypes())
                    .Where(type => !type.IsAbstract && typeof(Controller).IsAssignableFrom(type))
                    .Select(type => new object[] {type.AssemblyQualifiedName!})
                    .ToArray();
            }
        }

        [MemberData(nameof(Data))]
        [Theory]
        public void ShouldHaveAnonymousOrAuthorize(string qualifiedName)
        {
            var controllerType = Type.GetType(qualifiedName);

            Assert.NotNull(controllerType);

            Type[] httpMethodAttributes = {typeof(AuthorizeAttribute), typeof(AllowAnonymousAttribute)};

            object[] attributes = controllerType!.GetCustomAttributes(true).Where(attribute =>
                    httpMethodAttributes.Any(authenticationAttribute => authenticationAttribute == attribute.GetType()))
                .ToArray();

            _testOutputHelper.WriteLine(
                $"Controller '{controllerType.Name}' anonymous or authorization attributes: {attributes.Length}, expected is 1");

            Assert.NotEmpty(attributes);
            Assert.Single(attributes);
        }
    }
}