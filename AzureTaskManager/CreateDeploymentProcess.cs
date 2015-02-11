using Helpers;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace AzureTaskManager
{
    public class CreateDeploymentProcess
    {

        public static void CreateDeploymentSteps(string message, TextWriter log)
        {
            RootCreateDeploymentObject r = JsonConvert.DeserializeObject<RootCreateDeploymentObject>(message);

            log.WriteLine("Received message to deploy {0}", r.createdeployment.Service.ServiceName);

            // get the subscription details from configuration
            Subscription sub = Subscriptions.Instance.Get(r.createdeployment.SubscriptionName);
            if (sub == null)
            {
                string msg = string.Format("Subscription name {0} not found in configuration file.", r.createdeployment.SubscriptionName);
                Common.LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }

            // create credentials object based on management certificate
            CertificateCloudCredentials creds = new CertificateCloudCredentials(sub.SubscriptionId, sub.MgtCertificate);

            // see if the cloud service already exists
            bool nameIsAvailable = Common.CheckServiceNameAvailability(r.createdeployment.Service.ServiceName, creds);

            // if not and create if not exists, create it.
            if (nameIsAvailable && r.createdeployment.Service.CreateServiceIfNotExist)
            {
                log.WriteLine("Creating hosted service: {0}", r.createdeployment.Service.ServiceName);
                HttpStatusCode code = Common.CreateService(creds, r, sub, log);
                log.WriteLine("Code returned from CreateService: {0}", code.ToString());
            }

            // see if there's a cert to install
            if (!string.IsNullOrEmpty(r.createdeployment.Service.ServiceCertificate) && r.createdeployment.Service.InstallCertificateIfNotPresent)
            {
                log.WriteLine("Installing service certificate into hosted service: {0}", r.createdeployment.Service.ServiceName);
                HttpStatusCode code = CreateCertificate(creds, r, sub, log);
                log.WriteLine("Code returned from CreateCertificate: {0}", code.ToString());
            }

            // get the storage account for the subscription
            string acctName = Subscriptions.Instance.Get(r.createdeployment.SubscriptionName).PackageStorageAcct;

            // see if the service config file exists (this is because CreateDeployment doesn't have very good messaging)
            if (!Storage.BlobExists(acctName, r.createdeployment.Package.ConfigFileName))
            {
                string msg = string.Format("Service configuration file {0} does not exist", r.createdeployment.Package.ConfigFileName);
                Common.LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }

            // see if the package file exists (this is because CreateDeployment doesn't have very good messaging)
            if (!Storage.BlobExists(acctName, r.createdeployment.Package.PackageName))
            {
                string msg = string.Format("Service package file {0} does not exist", r.createdeployment.Package.PackageName);
                Common.LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }

            // create or update the deployment   (TODO: add code to check if service already occupied)
            string requestId = null;
            if (r.createdeployment.Service.UpdateIfAlreadyPresent && DeploymentExists(creds, r, sub, log))
            {
                requestId = UpdateDeployment(creds, r, sub, log);
            }
            else
            {
                requestId = CreateDeployment(creds, r, sub, log);
            }

            if (string.IsNullOrEmpty(requestId))
            {
                string msg = string.Format("Deployment of service {0} did not succeed.", r.createdeployment.Service.ServiceName);
                Common.LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }
            else
            {
                string msg = string.Format("Deployment of service {0} succeeded with request ID: {1}.", r.createdeployment.Service.ServiceName, requestId);
                Common.LogSuccess(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }

        }


        private static HttpStatusCode CreateCertificate(CertificateCloudCredentials creds, RootCreateDeploymentObject r, Subscription sub, TextWriter log)
        {
            byte[] certBytes = Storage.GetBlockBlobBytes(r.createdeployment.Service.ServiceCertificate);

            ServiceCertificateCreateParameters parms = new ServiceCertificateCreateParameters
            {
                CertificateFormat = CertificateFormat.Pfx,
                Data = certBytes,
                Password = r.createdeployment.Service.ServiceCertificatePassword
            };

            var client = new ComputeManagementClient(creds);
            var resp = client.ServiceCertificates.Create(r.createdeployment.Service.ServiceName, parms);

            return resp.HttpStatusCode;
        }

        public static bool DeploymentExists(SubscriptionCloudCredentials creds, RootCreateDeploymentObject root, Subscription sub, TextWriter log)
        {
            DeploymentGetResponse resp = null;
            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    resp = client.Deployments.GetBySlot(
                        root.createdeployment.Service.ServiceName,
                        (DeploymentSlot) Enum.Parse(typeof(DeploymentSlot), root.createdeployment.Service.Slot));     
                }
            }
            catch (CloudException ce)
            {
                log.WriteLine("Resource not found: {0} deployment does not exist for service {1}.", root.createdeployment.Service.Slot, root.createdeployment.Service.ServiceName);
                log.WriteLine("ErrorCode: {0}, Response.StatusCode: {1}", ce.ErrorCode, ce.Response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception {1} getting deployment: {0}", ex.Message, ex.ToString());
                Common.LogExit(msg, root.createdeployment.Service.ServiceName, log);
                return false;
            }

            return true;
        }

        public static string UpdateDeployment(SubscriptionCloudCredentials creds, RootCreateDeploymentObject root, Subscription sub, TextWriter log)
        {
            try
            {
                string configFile = Storage.GetServiceConfigFile(sub.PackageStorageAcct, root.createdeployment.Package.ConfigFileName);
                string label = root.createdeployment.Service.Label;
                string name = root.createdeployment.Service.Label;
                Uri packageUri = Storage.GetBlobUri(sub.PackageStorageAcct, sub.PackageContainerName, root.createdeployment.Package.PackageName);

                using (var client = new ComputeManagementClient(creds))
                {
                    OperationStatusResponse resp = client.Deployments.UpgradeBySlot(root.createdeployment.Service.ServiceName,
                        (DeploymentSlot)Enum.Parse(typeof(DeploymentSlot), root.createdeployment.Service.Slot),
                        new DeploymentUpgradeParameters
                        {
                            Configuration = configFile,
                            Force = false,
                            Label = label,
                            Mode = DeploymentUpgradeMode.Auto,
                            PackageUri = packageUri
                        });
                    return resp.RequestId;
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception updating deployment: {0}", ex.Message);
                Common.LogExit(msg, root.createdeployment.Service.ServiceName, log);
                return null;
            }
        }

        public static string CreateDeployment(SubscriptionCloudCredentials creds, RootCreateDeploymentObject root, Subscription sub, TextWriter log)
        {
            try
            {
                string configFile = Storage.GetServiceConfigFile(sub.PackageStorageAcct, root.createdeployment.Package.ConfigFileName);
                string label = root.createdeployment.Service.Label;
                string name = root.createdeployment.Service.Label;
                Uri packageUri = Storage.GetBlobUri(sub.PackageStorageAcct, sub.PackageContainerName, root.createdeployment.Package.PackageName);

                using (var client = new ComputeManagementClient(creds))
                {
                    OperationStatusResponse resp = client.Deployments.Create(root.createdeployment.Service.ServiceName, DeploymentSlot.Production,
                        new DeploymentCreateParameters
                        {
                            Configuration = configFile,
                            Label = label,
                            Name = name,
                            PackageUri = packageUri,
                            StartDeployment = true,
                            TreatWarningsAsError = false
                        });
                    return resp.RequestId;
                }
            }
            catch (Exception ex)
            {
                // get a 404 if the cloud service doesn't exist
                string msg = string.Format("Exception creating deployment: {0}", ex.Message);
                Common.LogExit(msg, root.createdeployment.Service.ServiceName, log);
                return null;
            }
        }

        
    }
}
