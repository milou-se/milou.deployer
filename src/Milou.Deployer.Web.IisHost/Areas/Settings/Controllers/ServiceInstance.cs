using System;
using System.Text.Json.Serialization;

namespace Milou.Deployer.Web.IisHost.Areas.Settings.Controllers
{
    public class ServiceInstance
    {
        public ServiceInstance(Type registrationType, object instance, Type? module)
        {
            RegistrationType = registrationType.FullName ?? registrationType.Name;
            Instance = instance.ToString() ?? registrationType.FullName ?? registrationType.Name;
            Module = module?.FullName ?? module?.Name;
        }

        [JsonConstructor]
        [Newtonsoft.Json.JsonConstructor]
        public ServiceInstance(string registrationType, string instance, string? module = null)
        {
            RegistrationType = registrationType;
            Instance = instance;
            Module = module;
        }

        public string RegistrationType { get; }
        public string Instance { get; }
        public string? Module { get; }
    }
}