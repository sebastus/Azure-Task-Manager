using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace Helpers
{
    public class Common
    {
        public static bool CheckServiceNameAvailability(string serviceName, SubscriptionCloudCredentials creds)
        {
            using (var client = new ComputeManagementClient(creds))
            {
                HostedServiceCheckNameAvailabilityResponse resp = client.HostedServices.CheckNameAvailability(serviceName);
                return resp.IsAvailable;
            }
        }

        public static void LogExit(string message, string consoleMessage, string serviceName, TextWriter log)
        {
            log.WriteLine(message);
            Console.WriteLine(consoleMessage);
        }

        public static void LogExit(string message, string serviceName, TextWriter log)
        {
            log.WriteLine(message);
            Console.WriteLine("Operation failed.  Check the log for details.");
        }

        public static void LogSuccess(string message, string consoleMessage, string serviceName, TextWriter log)
        {
            log.WriteLine(message);
            Console.WriteLine(consoleMessage);
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

        public static HttpStatusCode CreateService(SubscriptionCloudCredentials creds, RootCreateVMObject root, Subscription sub, TextWriter log)
        {
            try
            {
                using (var client = new ComputeManagementClient(creds))
                {
                    var resp = client.HostedServices.Create(new HostedServiceCreateParameters
                    {
                        Label = sub.PackageStorageAcct,
                        Location = root.createvm.Service.Location,
                        ServiceName = root.createvm.Service.ServiceName
                    });
                    return resp.StatusCode;
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception creating cloud service: {0}", ex.Message);
                LogExit(msg, root.createvm.Service.ServiceName, log);
                return HttpStatusCode.BadRequest;
            }
        }
    }
}
