using Helpers;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Models;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using System.Threading;
using System.Threading.Tasks;

namespace AzureTaskManager
{

    public class Functions
    {
        private static Storage storageObject;
        private static XNamespace wa = "http://schemas.microsoft.com/windowsazure";
        private static XNamespace ns1 = "http://www.w3.org/2001/XMLSchema-instance";

        static Functions()
        {
            storageObject = new Storage();
        }

        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessCreateDeploymentMessage([QueueTrigger("createdeployment")] string message, TextWriter log)
        {
            RootCreateDeploymentObject r = JsonConvert.DeserializeObject<RootCreateDeploymentObject>(message);

            log.WriteLine("Received message to deploy {0}", r.createdeployment.Service.ServiceName);

            // get the subscription details from configuration
            Subscription sub = Subscriptions.Instance.Get(r.createdeployment.SubscriptionName);
            if (sub == null)
            {
                string msg = string.Format("Subscription name {0} not found in configuration file.", r.createdeployment.SubscriptionName);
                LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }
            
            // create credentials object based on management certificate
            CertificateCloudCredentials creds = new CertificateCloudCredentials(sub.SubscriptionId, sub.MgtCertificate);

            // see if the cloud service already exists
            bool nameIsAvailable = CheckServiceNameAvailability(r.createdeployment.Service.ServiceName, creds);

            // if not and create if not exists, create it.
            if (nameIsAvailable && r.createdeployment.Service.CreateServiceIfNotExist)
            {
                log.WriteLine("Creating hosted service: {0}", r.createdeployment.Service.ServiceName);
                HttpStatusCode code = CreateService(creds, r, sub, log);
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
            if (!storageObject.BlobExists(acctName, r.createdeployment.Package.ConfigFileName))
            {
                string msg = string.Format("Service configuration file {0} does not exist", r.createdeployment.Package.ConfigFileName);
                LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }

            // see if the package file exists (this is because CreateDeployment doesn't have very good messaging)
            if (!storageObject.BlobExists(acctName, r.createdeployment.Package.PackageName))
            {
                string msg = string.Format("Service package file {0} does not exist", r.createdeployment.Package.PackageName);
                LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }

            // create the deployment
            string requestId = CreateDeployment(creds, r, sub, log);
            if (string.IsNullOrEmpty(requestId))
            {
                string msg = string.Format("Deployment of service {0} did not succeed.", r.createdeployment.Service.ServiceName);
                LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }
            else
            {
                string msg = string.Format("Deployment of service {0} succeeded with request ID: {1}.", r.createdeployment.Service.ServiceName, requestId);
                LogExit(msg, r.createdeployment.Service.ServiceName, log);
                return;
            }

        }

        private static HttpStatusCode CreateCertificate(CertificateCloudCredentials creds, RootCreateDeploymentObject r, Subscription sub, TextWriter log)
        {
            byte[] certBytes = storageObject.GetBlockBlobBytes(r.createdeployment.Service.ServiceCertificate);
            
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

        public static bool CheckServiceNameAvailability(string serviceName, SubscriptionCloudCredentials creds)
        {
            using (var client = new ComputeManagementClient(creds))
            {
                HostedServiceCheckNameAvailabilityResponse resp = client.HostedServices.CheckNameAvailability(serviceName);
                return resp.IsAvailable;
            }
        }

        public static HttpStatusCode CreateService(SubscriptionCloudCredentials creds, RootCreateDeploymentObject root, Subscription sub, TextWriter log)
        {
            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    var resp = client.HostedServices.Create(new HostedServiceCreateParameters
                    {
                        Label = sub.PackageStorageAcct,
                        Location = root.createdeployment.Service.Location,
                        ServiceName = root.createdeployment.Service.ServiceName
                    });
                    return resp.StatusCode;
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception creating cloud service: {0}", ex.Message);
                LogExit(msg, root.createdeployment.Service.ServiceName, log);
                return HttpStatusCode.BadRequest;
            }
        }

        public static string CreateDeployment(SubscriptionCloudCredentials creds, RootCreateDeploymentObject root, Subscription sub, TextWriter log)
        {
            try
            {
                string configFile = storageObject.GetServiceConfigFile(sub.PackageStorageAcct, root.createdeployment.Package.ConfigFileName);
                string label = root.createdeployment.Service.Label;
                string name = root.createdeployment.Service.Label;
                Uri packageUri = GetBlobUri(sub.PackageStorageAcct, sub.PackageContainerName, root.createdeployment.Package.PackageName);

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
                LogExit(msg, root.createdeployment.Service.ServiceName, log);
                return null;
            }
        }

        private static Uri GetBlobUri(string storageAccountName, string containerName, string fileName)
        {            
            return new Uri(string.Format("http://{0}.blob.core.windows.net/{1}/{2}", storageAccountName, containerName, fileName));
        }

        private static void LogExit(string message, string serviceName, TextWriter log)
        {
            log.WriteLine(message);
            Console.WriteLine("Command to deploy service {0} failed, check log for details.", serviceName);
        }
    }
}
