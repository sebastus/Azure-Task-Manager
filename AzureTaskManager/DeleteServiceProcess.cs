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
    public class DeleteServiceProcess
    {
        public static void DeleteServiceSteps(string message, TextWriter log)
        {
            RootDeleteServiceObject r = JsonConvert.DeserializeObject<RootDeleteServiceObject>(message);

            log.WriteLine("Received message to delete service {0}", r.deleteservice.Service.ServiceName);

            // get the subscription details from configuration
            Subscription sub = Subscriptions.Instance.Get(r.deleteservice.SubscriptionName);
            if (sub == null)
            {
                string msg = string.Format("Subscription name {0} not found in configuration file.", r.deleteservice.SubscriptionName);
                Common.LogExit(msg, r.deleteservice.Service.ServiceName, log);
                return;
            }

            // create credentials object based on management certificate
            CertificateCloudCredentials creds = new CertificateCloudCredentials(sub.SubscriptionId, sub.MgtCertificate);

            // see if the cloud service exists
            bool nameIsAvailable = Common.CheckServiceNameAvailability(r.deleteservice.Service.ServiceName, creds);

            // if so, nothing to do.
            if (nameIsAvailable)
            {
                log.WriteLine("Hosted service {0} does not exist.  Nothing to do.", r.deleteservice.Service.ServiceName);
                return;
            }

            if (r.deleteservice.Service.ConfirmDeleteIfOccupied)
            {
                bool deploymentExists = DeploymentExists(creds, r, sub, log);

                if (deploymentExists)
                {
                    log.WriteLine("A deployment exists in service {0} and no confirmation to delete.  Nothing to do.", r.deleteservice.Service.ServiceName);
                    return;
                }
            }

            string requestId = DeleteService(creds, r, sub, log);

            if (string.IsNullOrEmpty(requestId))
            {
                string msg = string.Format("Deletion of service {0} did not succeed.",
                    r.deleteservice.Service.ServiceName);
                string consoleMessage = string.Format("Command to delete service {0} failed.  Check the log for details.",
                    r.deleteservice.Service.ServiceName);
                Common.LogExit(msg, consoleMessage, r.deleteservice.Service.ServiceName, log);
                return;
            }
            else
            {
                string msg = string.Format("Deletion of service {0} succeeded with request ID: {1}.",
                    r.deleteservice.Service.ServiceName,
                    requestId);
                string consoleMessage = string.Format("Command to delete service {0} succeeded.  Check the log for details.",
                    r.deleteservice.Service.ServiceName);
                Common.LogSuccess(msg, consoleMessage, r.deleteservice.Service.ServiceName, log);
                return;
            }
        }

        private static string DeleteService(CertificateCloudCredentials creds, RootDeleteServiceObject r, Subscription sub, TextWriter log)
        {
            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    OperationResponse resp = client.HostedServices.Delete(
                        r.deleteservice.Service.ServiceName);
                    return resp.RequestId;
                }

            }
            catch (Exception ex)
            {
                // get a 404 if the cloud service doesn't exist
                string msg = string.Format("Exception deleting service: {0}", ex.Message);
                Common.LogExit(msg, r.deleteservice.Service.ServiceName, log);
                return null;
            }
        }

        private static bool DeploymentExists(SubscriptionCloudCredentials creds, RootDeleteServiceObject root, Subscription sub, TextWriter log)
        {
            DeploymentGetResponse resp = null;
            bool productionDeploymentExists = true;
            bool stagingDeploymentExists = true;
            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    resp = client.Deployments.GetBySlot(
                        root.deleteservice.Service.ServiceName,
                        DeploymentSlot.Production);
                }
            }
            catch (CloudException ce)
            {
                log.WriteLine("Resource not found: production deployment does not exist for service {0}.", root.deleteservice.Service.ServiceName);
                log.WriteLine("ErrorCode: {0}, Response.StatusCode: {1}", ce.ErrorCode, ce.Response.StatusCode);
                productionDeploymentExists = false;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception {1} getting deployment: {0}", ex.Message, ex.ToString());
                Common.LogExit(msg, root.deleteservice.Service.ServiceName, log);
                productionDeploymentExists = false;
            }

            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    resp = client.Deployments.GetBySlot(
                        root.deleteservice.Service.ServiceName,
                        DeploymentSlot.Staging);
                }
            }
            catch (CloudException ce)
            {
                log.WriteLine("Resource not found: staging deployment does not exist for service {0}.",  root.deleteservice.Service.ServiceName);
                log.WriteLine("ErrorCode: {0}, Response.StatusCode: {1}", ce.ErrorCode, ce.Response.StatusCode);
                stagingDeploymentExists = false;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception {1} getting deployment: {0}", ex.Message, ex.ToString());
                Common.LogExit(msg, root.deleteservice.Service.ServiceName, log);
                stagingDeploymentExists = false;
            }

            return (productionDeploymentExists || stagingDeploymentExists);
        }


    }
}
