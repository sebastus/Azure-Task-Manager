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
    public class ShutdownVMsProcess
    {

        public static void ShutdownVMsSteps(string message, TextWriter log)
        {
            RootShutdownVMsObject r = JsonConvert.DeserializeObject<RootShutdownVMsObject>(message);

            log.WriteLine("Received message to deploy {0}", r.shutdownvms.Service.ServiceName);

            // get the subscription details from configuration
            Subscription sub = Subscriptions.Instance.Get(r.shutdownvms.SubscriptionName);
            if (sub == null)
            {
                string msg = string.Format("Subscription name {0} not found in configuration file.", r.shutdownvms.SubscriptionName);
                Common.LogExit(msg, r.shutdownvms.Service.ServiceName, log);
                return;
            }

            // create credentials object based on management certificate
            CertificateCloudCredentials creds = new CertificateCloudCredentials(sub.SubscriptionId, sub.MgtCertificate);

            // see if the cloud service exists
            bool nameIsAvailable = Common.CheckServiceNameAvailability(r.shutdownvms.Service.ServiceName, creds);

            // if so, nothing to do.
            if (nameIsAvailable)
            {
                log.WriteLine("Hosted service {0} does not exist.", r.shutdownvms.Service.ServiceName);
                return;
            }

            string requestId = ShutdownVMs(creds, r, sub, log);

            if (string.IsNullOrEmpty(requestId))
            {
                string msg = string.Format("Shutdown of VMs on service {0} did not succeed.", r.shutdownvms.Service.ServiceName);
                string consoleMsg = string.Format("Shutdown of VMs on service {0} failed.  Check the log for details.", r.shutdownvms.Service.ServiceName);
                Common.LogExit(msg, consoleMsg, r.shutdownvms.Service.ServiceName, log);
                return;
            }
            else
            {
                string msg = string.Format("Shutdown of VMs on service {0} succeeded with request ID: {1}.", r.shutdownvms.Service.ServiceName, requestId);
                string consoleMsg = string.Format("Shutdown of VMs on service {0} succeeded.  Check the log for details.", r.shutdownvms.Service.ServiceName);
                Common.LogSuccess(msg, consoleMsg, r.shutdownvms.Service.ServiceName, log);
                return;
            }
        }

        public static string ShutdownVMs(SubscriptionCloudCredentials creds, RootShutdownVMsObject root, Subscription sub, TextWriter log)
        {
            List<string> roles = new List<string>();
            foreach (JSONShutdownVMsVM vm in root.shutdownvms.VM)
            {
                roles.Add(vm.Name);
            }

            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    OperationStatusResponse resp = client.VirtualMachines.ShutdownRoles(
                        root.shutdownvms.Service.ServiceName,
                        root.shutdownvms.Service.DeploymentName,
                        new VirtualMachineShutdownRolesParameters
                        {
                            PostShutdownAction = (PostShutdownAction) Enum.Parse(typeof(PostShutdownAction), root.shutdownvms.PostShutDownAction),
                            Roles = roles
                        });
                    return resp.RequestId;
                }

            }
            catch (Exception ex)
            {
                // get a 404 if the cloud service doesn't exist
                string msg = string.Format("Exception deleting VMs: {0}", ex.Message);
                Common.LogExit(msg, root.shutdownvms.Service.ServiceName, log);
                return null;
            }
        }

    }
}
