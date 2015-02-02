using System.Collections;
using System.Collections.Generic;

namespace Helpers
{
    public class JSONSubscription
    {
        public string SubscriptionName { get; set; }
        public string SubscriptionId { get; set; }
        public string MgtCertFileName { get; set; }
        public string MgtCertPassword { get; set; }
        public string PackageStorageAcct { get; set; }
        public string PackageStorageKey { get; set; }
        public string PackageContainerName { get; set; }
    }

    public class RootConfigurationObject
    {
        public List<JSONSubscription> Subscriptions { get; set; }
    }


    public class JSONCreateDeploymentPackage
    {
        public string PackageName { get; set; }
        public string ConfigFileName { get; set; }
    }

    public class JSONHostedService
    {
        public string Label { get; set; }
        public string ServiceName { get; set; }
        public bool CreateServiceIfNotExist { get; set; }
        public string Location { get; set; }
        public string Slot { get; set; }
    }

    public class JSONCreateDeployment
    {
        public string SubscriptionName { get; set; }
        public JSONCreateDeploymentPackage Package { get; set; }
        public JSONHostedService Service { get; set; }
    }

    public class RootCreateDeploymentObject
    {
        public JSONCreateDeployment createdeployment { get; set; }
    }
}
