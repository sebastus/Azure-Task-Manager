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
    public class CreateVMProcess
    {

        public static void CreateVMSteps(string message, TextWriter log)
        {
            RootCreateVMObject r = JsonConvert.DeserializeObject<RootCreateVMObject>(message);

            log.WriteLine("Received message to deploy {0}", r.createvm.VM.ServiceName);

            // get the subscription details from configuration
            Subscription sub = Subscriptions.Instance.Get(r.createvm.SubscriptionName);
            if (sub == null)
            {
                string msg = string.Format("Subscription name {0} not found in configuration file.", r.createvm.SubscriptionName);
                Common.LogExit(msg, r.createvm.VM.ServiceName, log);
                return;
            }

            // create credentials object based on management certificate
            CertificateCloudCredentials creds = new CertificateCloudCredentials(sub.SubscriptionId, sub.MgtCertificate);

            // see if the cloud service already exists
            bool nameIsAvailable = Common.CheckServiceNameAvailability(r.createvm.VM.ServiceName, creds);

            // if not and create if not exists, create it.
            if (nameIsAvailable && r.createvm.VM.CreateServiceIfNotExist)
            {
                log.WriteLine("Creating hosted service: {0}", r.createvm.VM.ServiceName);
                HttpStatusCode code = Common.CreateService(creds, r, sub, log);
                log.WriteLine("Code returned from CreateService: {0}", code.ToString());
            }


        }
    }
}
