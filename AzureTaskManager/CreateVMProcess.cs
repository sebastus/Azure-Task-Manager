﻿using Helpers;
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
    public class CreateVMProcess
    {

        public static void CreateVMSteps(string message, TextWriter log)
        {
            RootCreateVMObject r = JsonConvert.DeserializeObject<RootCreateVMObject>(message);

            log.WriteLine("Received message to deploy {0}", r.createvm.Service.ServiceName);

            // get the subscription details from configuration
            Subscription sub = Subscriptions.Instance.Get(r.createvm.SubscriptionName);
            if (sub == null)
            {
                string msg = string.Format("Subscription name {0} not found in configuration file.", r.createvm.SubscriptionName);
                Common.LogExit(msg, r.createvm.Service.ServiceName, log);
                return;
            }

            // create credentials object based on management certificate
            CertificateCloudCredentials creds = new CertificateCloudCredentials(sub.SubscriptionId, sub.MgtCertificate);

            // see if the cloud service already exists
            bool nameIsAvailable = Common.CheckServiceNameAvailability(r.createvm.Service.ServiceName, creds);

            // if not and create if not exists, create it.
            if (nameIsAvailable && r.createvm.Service.CreateServiceIfNotExist)
            {
                log.WriteLine("Creating hosted service: {0}", r.createvm.Service.ServiceName);
                HttpStatusCode code = Common.CreateService(creds, r, sub, log);
                log.WriteLine("Code returned from CreateService: {0}", code.ToString());
            }

            bool deploymentExists = DeploymentExists(creds, r, sub, log);
            string requestId = null;

            if (deploymentExists)
            {
                requestId = CreateVM(creds, r, sub, log);
            }
            else
            {
                requestId = CreateDeployment(creds, r, sub, log);
            }

            if (string.IsNullOrEmpty(requestId))
            {
                string msg = string.Format("Creation of VM on service {0} did not succeed.", r.createvm.Service.ServiceName);
                string consoleMsg = string.Format("Creation of VM on service {0} failed.  Check the log for details.", r.createvm.Service.ServiceName);
                Common.LogExit(msg, consoleMsg, r.createvm.Service.ServiceName, log);
                return;
            }
            else
            {
                string msg = string.Format("Creation of VM on service {0} succeeded with request ID: {1}.", r.createvm.Service.ServiceName, requestId);
                string consoleMsg = string.Format("Creation of VM on service {0} succeeded.  Check the log for details.", r.createvm.Service.ServiceName);
                Common.LogSuccess(msg, consoleMsg, r.createvm.Service.ServiceName, log);
                return;
            }
        }

        private static string CreateDeployment(SubscriptionCloudCredentials creds, RootCreateVMObject root, Subscription sub, TextWriter log)
        {
            InputEndpoint rdEndpoint = new InputEndpoint();
            rdEndpoint.EnableDirectServerReturn = false;
            rdEndpoint.EndpointAcl = null;
            rdEndpoint.LocalPort = 3389;
            rdEndpoint.Port = 3389;
            rdEndpoint.Name = "RD";
            rdEndpoint.Protocol = "TCP";

            List<Role> roles = new List<Role>();
            foreach (JSONVM vm in root.createvm.VM)
            {
                ConfigurationSet set = new ConfigurationSet();
                set.AdminPassword = vm.ConfigurationSet.AdminPassword;
                set.AdminUserName = vm.ConfigurationSet.AdminUserName;
                set.ComputerName = vm.ConfigurationSet.ComputerName;  // Host Name in portal
                set.ConfigurationSetType = ConfigurationSetTypes.WindowsProvisioningConfiguration;
                set.ResetPasswordOnFirstLogon = false;

                OSVirtualHardDisk disk = new OSVirtualHardDisk();
                disk.HostCaching = VirtualHardDiskHostCaching.ReadWrite;
                disk.Label = "osdisk";
                disk.SourceImageName = vm.OSVirtualHardDisk.SourceImageName;
                string mediaLink = string.Format("https://{0}.blob.core.windows.net/{1}/{2}-{3}-osdisk.vhd",
                    vm.OSVirtualHardDisk.StorageAccount,
                    vm.OSVirtualHardDisk.Container,
                    root.createvm.Service.ServiceName,
                    vm.ConfigurationSet.ComputerName);
                disk.MediaLink = new Uri(mediaLink);

                Role r = new Role();
                r.ConfigurationSets.Add(set);
                r.Label = vm.Name + "-label";
                r.OSVirtualHardDisk = disk;
                r.RoleName = vm.Name;  // VM Name in portal
                r.RoleSize = vm.Size;  // permitted values = enum VirtualMachineRoleSize
                r.RoleType = VirtualMachineRoleType.PersistentVMRole.ToString();
                r.OSVersion = "Windows Server 2012 R2"; // appears to be simply a string, no function
                r.ProvisionGuestAgent = true;
                
                roles.Add(r);
            }            

            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    OperationStatusResponse resp = client.VirtualMachines.CreateDeployment(root.createvm.Service.ServiceName,
                        new VirtualMachineCreateDeploymentParameters
                        {
                            DeploymentSlot = DeploymentSlot.Production,
                            Label = root.createvm.Service.ServiceName + "-label",
                            Name = root.createvm.Service.DeploymentName,         // this goes into the name of the status file for the VHD, it's also used for future operations on this VM
                            Roles = roles
                        });
                    return resp.RequestId;
                }

            }
            catch (Exception ex)
            {
                // get a 404 if the cloud service doesn't exist
                string msg = string.Format("Exception creating VM: {0}", ex.Message);
                Common.LogExit(msg, root.createvm.Service.ServiceName, log);
                return null;
            }

        }

        private static string CreateVM(SubscriptionCloudCredentials creds, RootCreateVMObject root, Subscription sub, TextWriter log)
        {
            InputEndpoint rdEndpoint = new InputEndpoint();
            rdEndpoint.EnableDirectServerReturn = false;
            rdEndpoint.EndpointAcl = null;
            rdEndpoint.LocalPort = 3389;
            rdEndpoint.Port = 3389;
            rdEndpoint.Name = "RD";
            rdEndpoint.Protocol = "TCP";

            ConfigurationSet set = new ConfigurationSet();
            set.AdminPassword = root.createvm.VM[0].ConfigurationSet.AdminPassword;
            set.AdminUserName = root.createvm.VM[0].ConfigurationSet.AdminUserName;
            set.ComputerName = root.createvm.VM[0].ConfigurationSet.ComputerName;  // Host Name in portal
            set.ConfigurationSetType = ConfigurationSetTypes.WindowsProvisioningConfiguration;
            set.ResetPasswordOnFirstLogon = false;

            List<ConfigurationSet> sets = new List<ConfigurationSet>();
            sets.Add(set);

            OSVirtualHardDisk disk = new OSVirtualHardDisk();
            disk.HostCaching = VirtualHardDiskHostCaching.ReadWrite;
            disk.Label = "osdisk";
            disk.SourceImageName = root.createvm.VM[0].OSVirtualHardDisk.SourceImageName;
            string mediaLink = string.Format("https://{0}.blob.core.windows.net/{1}/{2}-{3}-osdisk.vhd",
                root.createvm.VM[0].OSVirtualHardDisk.StorageAccount,
                root.createvm.VM[0].OSVirtualHardDisk.Container,
                root.createvm.Service.ServiceName,
                root.createvm.VM[0].ConfigurationSet.ComputerName);
            disk.MediaLink = new Uri(mediaLink);

            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    OperationStatusResponse resp = client.VirtualMachines.Create(
                        root.createvm.Service.ServiceName,
                        root.createvm.Service.DeploymentName,
                        new VirtualMachineCreateParameters
                        {
                            ConfigurationSets = sets,
                            OSVirtualHardDisk = disk,
                            RoleName = root.createvm.VM[0].Name, // this goes into the name of the status file for the VHD, it's also used for future operations on this VM
                            RoleSize = root.createvm.VM[0].Size,
                            ProvisionGuestAgent = true
                        });
                    return resp.RequestId;
                }

            }
            catch (Exception ex)
            {
                // get a 404 if the cloud service doesn't exist
                string msg = string.Format("Exception creating VM: {0}", ex.Message);
                Common.LogExit(msg, root.createvm.Service.ServiceName, log);
                return null;
            }

        }
        private static bool DeploymentExists(SubscriptionCloudCredentials creds, RootCreateVMObject root, Subscription sub, TextWriter log)
        {
            DeploymentGetResponse resp = null;
            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    resp = client.Deployments.GetByName(
                        root.createvm.Service.ServiceName,
                        root.createvm.Service.DeploymentName);
                }
            }
            catch (CloudException ce)
            {
                log.WriteLine("Resource not found: {0} deployment does not exist for service {1}.", root.createvm.Service.DeploymentName, root.createvm.Service.ServiceName);
                log.WriteLine("ErrorCode: {0}, Response.StatusCode: {1}", ce.ErrorCode, ce.Response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception {1} getting deployment: {0}", ex.Message, ex.ToString());
                Common.LogExit(msg, root.createvm.Service.ServiceName, log);
                return false;
            }

            return true;
        }

    }
}
