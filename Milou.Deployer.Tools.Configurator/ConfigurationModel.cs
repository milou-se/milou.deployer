using System.ComponentModel.DataAnnotations;

namespace Milou.Deployer.Tools.Configurator
{
    public class ConfigurationModel
    {
        [Required] public string MartenConnectionString { get; set; }
    }
}