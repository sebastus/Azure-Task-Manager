using Helpers;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;


namespace AzureTaskManager
{
    public class DeleteDeploymentProcess
    {
        public static void DeleteDeploymentSteps(string message, TextWriter log)
        {
            RootDeleteDeploymentObject r = JsonConvert.DeserializeObject<RootDeleteDeploymentObject>(message);

            log.WriteLine("Received message to delete {1} deployment on service {0}", r.deletedeployment.Service.ServiceName, r.deletedeployment.Service.Slot);

            // get the subscription details from configuration
            Subscription sub = Subscriptions.Instance.Get(r.deletedeployment.SubscriptionName);
            if (sub == null)
            {
                string msg = string.Format("Subscription name {0} not found in configuration file.", r.deletedeployment.SubscriptionName);
                Common.LogExit(msg, r.deletedeployment.Service.ServiceName, log);
                return;
            }

            // create credentials object based on management certificate
            CertificateCloudCredentials creds = new CertificateCloudCredentials(sub.SubscriptionId, sub.MgtCertificate);

            // see if the cloud service exists
            bool nameIsAvailable = Common.CheckServiceNameAvailability(r.deletedeployment.Service.ServiceName, creds);

            // if so, nothing to do.
            if (nameIsAvailable)
            {
                log.WriteLine("Hosted service {0} does not exist.  Nothing to do.", r.deletedeployment.Service.ServiceName);
                return;
            }

            bool deploymentExists = DeploymentExists(creds, r, sub, log);

            if (!deploymentExists)
            {
                log.WriteLine("{0} deployment does not exist.  Nothing to do.", r.deletedeployment.Service.Slot);
                return;
            }

            string requestId = DeleteDeployment(creds, r, sub, log);

            if (string.IsNullOrEmpty(requestId))
            {
                string msg = string.Format("Deletion of {1} deployment on service {0} did not succeed.", 
                    r.deletedeployment.Service.ServiceName, 
                    r.deletedeployment.Service.Slot);
                string consoleMessage = string.Format("Command to delete {0} deployment on service {1} failed.  Check the log for details.",
                    r.deletedeployment.Service.Slot,
                    r.deletedeployment.Service.ServiceName);
                Common.LogExit(msg, consoleMessage, r.deletedeployment.Service.ServiceName, log);
                return;
            }
            else
            {
                string msg = string.Format("Deletion of {1} deployment on service {0} succeeded with request ID: {2}.", 
                    r.deletedeployment.Service.ServiceName, 
                    r.deletedeployment.Service.Slot,
                    requestId);
                string consoleMessage = string.Format("Command to delete {0} deployment on service {1} succeeded.  Check the log for details.",
                    r.deletedeployment.Service.Slot,
                    r.deletedeployment.Service.ServiceName); 
                Common.LogSuccess(msg, consoleMessage, r.deletedeployment.Service.ServiceName, log);
                return;
            }
        }

        public static bool DeploymentExists(SubscriptionCloudCredentials creds, RootDeleteDeploymentObject root, Subscription sub, TextWriter log)
        {
            DeploymentGetResponse resp = null;
            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    resp = client.Deployments.GetBySlot(
                        root.deletedeployment.Service.ServiceName,
                        (DeploymentSlot)Enum.Parse(typeof(DeploymentSlot), root.deletedeployment.Service.Slot));
                }
            }
            catch (CloudException ce)
            {
                log.WriteLine("Resource not found: {0} deployment does not exist for service {1}.", root.deletedeployment.Service.Slot, root.deletedeployment.Service.ServiceName);
                log.WriteLine("ErrorCode: {0}, Response.StatusCode: {1}", ce.ErrorCode, ce.Response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception {1} getting deployment: {0}", ex.Message, ex.ToString());
                Common.LogExit(msg, root.deletedeployment.Service.ServiceName, log);
                return false;
            }

            return true;
        }

        public static string DeleteDeployment(SubscriptionCloudCredentials creds, RootDeleteDeploymentObject root, Subscription sub, TextWriter log)
        {
            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    OperationStatusResponse resp = client.Deployments.DeleteBySlot(
                        root.deletedeployment.Service.ServiceName,
                        (DeploymentSlot)Enum.Parse(typeof(DeploymentSlot), root.deletedeployment.Service.Slot));
                    return resp.RequestId;
                }

            }
            catch (Exception ex)
            {
                // get a 404 if the cloud service doesn't exist
                string msg = string.Format("Exception deleting deployment: {0}", ex.Message);
                Common.LogExit(msg, root.deletedeployment.Service.ServiceName, log);
                return null;
            }
        }
    }
}
