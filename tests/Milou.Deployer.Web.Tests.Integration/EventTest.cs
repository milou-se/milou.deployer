using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.Messaging;
using MediatR;

using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class EventTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public EventTest(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;

        public static IEnumerable<object[]> GetTestAssemblyTypes()
        {
            string[] assemblyNameStartsWith = { "Milou" };
            var filteredAssemblies = ApplicationAssemblies.FilteredAssemblies(useCache: false,
                assemblyNameStartsWith: assemblyNameStartsWith);

            var allTypes = filteredAssemblies.SelectMany(a => a.GetExportedTypes()).ToArray();

            var requestTypes = allTypes.Where(type => type.IsPublic && !type.IsAbstract && typeof(INotification).IsAssignableFrom(type));
            foreach (var type in requestTypes)
            {
                yield return new object[] { type };
            }
        }

        [MemberData(nameof(GetTestAssemblyTypes))]
        [Theory]
        public void NotificationImplementationBeEvent(Type requestType)
        {
            _outputHelper.WriteLine(requestType.FullName);
            bool isEvent = typeof(IEvent).IsAssignableFrom(requestType);

            Assert.True(isEvent);
        }
    }
}