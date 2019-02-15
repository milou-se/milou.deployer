namespace Milou.Deployer.Core.Deployment
{
    public static class DeploymentConstants
    {
        public const string EnvironmentLiteral = "Environment";

        public const string EnvironmentPackagePattern =
            "{Name}." + EnvironmentLiteral + ".{EnvironmentName}.{action}.{extension}";

        public const string AppOfflineHtm = "App_Offline.htm";
    }
}
