using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Messaging;
using MediatR;
using Xunit;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class CommandTest
    {
        public static IEnumerable<object[]> GetTestAssemblyTypes()
        {
            string[] assemblyNameStartsWith = { "Milou" };
            var filteredAssemblies = ApplicationAssemblies.FilteredAssemblies(assemblyNameStartsWith: assemblyNameStartsWith,
useCache: false);

            var allTypes = filteredAssemblies.SelectMany(a => a.GetExportedTypes()).ToArray();

            var requestTypes = allTypes.Where(type => type.Closes(typeof(IRequest<>))
                                                      && type.IsPublic && !type.IsAbstract
                                                      && !type.GetInterfaces().Any(item => item.GenericTypeArguments.Length > 0
                                                      && item.GenericTypeArguments.Any(arg => arg == typeof(Unit))));
            foreach (var type in requestTypes)
            {
                yield return new object[] {type};
            }
        }

        [MemberData(nameof(GetTestAssemblyTypes))]
        [Theory]
        public void RequestImplementationMustBeCommandOrQuery(Type requestType)
        {
            bool isQuery = requestType.Closes(typeof(IQuery<>));

            bool isCommand = requestType.Closes(typeof(ICommand<>));

            Assert.True(isCommand ^ isQuery);
        }
    }
}