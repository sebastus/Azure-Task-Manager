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
    public class StartRolesProcess
    {
        public static void StartRolesSteps(string message, TextWriter log)
        {
            RootStartVMsObject r = JsonConvert.DeserializeObject<RootStartVMsObject>(message);

            log.WriteLine("Received message to start VM(s) on service {0}", r.startvms.Service.ServiceName);

            // get the subscription details from configuration
            Subscription sub = Subscriptions.Instance.Get(r.startvms.SubscriptionName);
            if (sub == null)
            {
                string msg = string.Format("Subscription name {0} not found in configuration file.",
                    r.startvms.SubscriptionName);
                Common.LogExit(msg, r.startvms.Service.ServiceName, log);
                return;
            }

            // create credentials object based on management certificate
            CertificateCloudCredentials creds = new CertificateCloudCredentials(sub.SubscriptionId, sub.MgtCertificate);

            // see if the cloud service exists
            bool nameIsAvailable = Common.CheckServiceNameAvailability(r.startvms.Service.ServiceName, creds);

            // if so, nothing to do.
            if (nameIsAvailable)
            {
                log.WriteLine("Hosted service {0} does not exist.  Nothing to do.", r.startvms.Service.ServiceName);
                return;
            }

            string requestId = StartVMs(creds, r, sub, log);

            if (string.IsNullOrEmpty(requestId))
            {
                string msg = string.Format("Start of VMs on service {0} did not succeed.", r.startvms.Service.ServiceName);
                string consoleMsg = string.Format("Start of VMs on service {0} failed.  Check the log for details.", r.startvms.Service.ServiceName);
                Common.LogExit(msg, consoleMsg, r.startvms.Service.ServiceName, log);
                return;
            }
            else
            {
                string msg = string.Format("Start of VMs on service {0} succeeded with request ID: {1}.", r.startvms.Service.ServiceName, requestId);
                string consoleMsg = string.Format("Start of VMs on service {0} succeeded.  Check the log for details.", r.startvms.Service.ServiceName);
                Common.LogSuccess(msg, consoleMsg, r.startvms.Service.ServiceName, log);
                return;
            }
        }

        private static string StartVMs(SubscriptionCloudCredentials creds, RootStartVMsObject root, Subscription sub, TextWriter log)
        {
            List<string> roles = new List<string>();
            foreach (JSONStartVMsVM vm in root.startvms.VM)
            {
                roles.Add(vm.Name);
            }

            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    OperationStatusResponse resp = client.VirtualMachines.StartRoles(
                        root.startvms.Service.ServiceName,
                        root.startvms.Service.DeploymentName,
                        new VirtualMachineStartRolesParameters
                        {
                            Roles = roles
                        });
                    return resp.RequestId;
                }

            }
            catch (Exception ex)
            {
                // get a 404 if the cloud service doesn't exist
                string msg = string.Format("Exception starting VMs: {0}", ex.Message);
                Common.LogExit(msg, root.startvms.Service.ServiceName, log);
                return null;
            }
        }
    }
}
