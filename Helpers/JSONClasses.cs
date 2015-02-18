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

    /// <summary>
    /// DeleteDeployment classes
    /// </summary>
    public class JSONDeleteDeploymentService
    {
        public string ServiceName { get; set; }
        public string Slot { get; set; }
    }

    public class JSONDeleteDeployment
    {
        public string SubscriptionName { get; set; }
        public JSONDeleteDeploymentService Service { get; set; }
    }

    public class RootDeleteDeploymentObject
    {
        public JSONDeleteDeployment deletedeployment { get; set; }
    }

    /// <summary>
    /// DeleteService classes
    /// </summary>
    public class JSONDeleteServiceService
    {
        public string ServiceName { get; set; }
        public bool ConfirmDeleteIfOccupied { get; set; }
    }

    public class JSONDeleteService
    {
        public string SubscriptionName { get; set; }
        public JSONDeleteServiceService Service { get; set; }
    }

    public class RootDeleteServiceObject
    {
        public JSONDeleteService deleteservice { get; set; }
    }

    /// <summary>
    /// Create PaaS Deployment classes (web role, worker role)
    /// </summary>
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
        public bool UpdateIfAlreadyPresent { get; set; }
        public string ServiceCertificate { get; set; }
        public string ServiceCertificatePassword { get; set; }
        public bool InstallCertificateIfNotPresent { get; set; }
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

    /// <summary>
    /// Create VM classes
    /// </summary>
    public class JSONVMService
    {
        public string ServiceName { get; set; }
        public string DeploymentName { get; set; }
        public bool CreateServiceIfNotExist { get; set; }
        public string Location { get; set; }
    }

    public class JSONConfigurationSet
    {
        public string AdminUserName { get; set; }
        public string AdminPassword { get; set; }
        public string ComputerName { get; set; }
    }

    public class JSONOSVirtualHardDisk
    {
        public string SourceImageName { get; set; }
        public string StorageAccount { get; set; }
        public string Container { get; set; }
    }

    public class JSONVM
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public JSONConfigurationSet ConfigurationSet { get; set; }
        public JSONOSVirtualHardDisk OSVirtualHardDisk { get; set; }
    }

    public class JSONCreateVM
    {
        public string SubscriptionName { get; set; }
        public JSONVMService Service { get; set; }
        public List<JSONVM> VM { get; set; }
    }

    public class RootCreateVMObject
    {
        public JSONCreateVM createvm { get; set; }
    }


    /// <summary>
    /// Classes for shutting down a set of VMs in the same cloud service (deployment)
    /// </summary>
    public class JSONShutdownVMsService
    {
        public string ServiceName { get; set; }
        public string DeploymentName { get; set; }
    }

    public class JSONShutdownVMsVM
    {
        public string Name { get; set; }
    }

    public class JSONShutdownVMs
    {
        public string SubscriptionName { get; set; }
        public JSONShutdownVMsService Service { get; set; }
        public List<JSONShutdownVMsVM> VM { get; set; }
        public string PostShutDownAction { get; set; }
    }

    public class RootShutdownVMsObject
    {
        public JSONShutdownVMs shutdownvms { get; set; }
    }

    /// <summary>
    /// Classes for shutting down a set of VMs in the same cloud service (deployment)
    /// </summary>
    public class JSONStartVMsService
    {
        public string ServiceName { get; set; }
        public string DeploymentName { get; set; }
    }

    public class JSONStartVMsVM
    {
        public string Name { get; set; }
    }

    public class JSONStartVMs
    {
        public string SubscriptionName { get; set; }
        public JSONStartVMsService Service { get; set; }
        public List<JSONStartVMsVM> VM { get; set; }
        public string PostStartAction { get; set; }
    }

    public class RootStartVMsObject
    {
        public JSONStartVMs startvms { get; set; }
    }
}
