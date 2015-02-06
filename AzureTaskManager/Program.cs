using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Helpers;
using Newtonsoft.Json;

namespace AzureTaskManager
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            RootConfigurationObject atmConfiguration = null;

            string configJSON = Storage.GetConfigurationFile();
            try
            {
                atmConfiguration = JsonConvert.DeserializeObject<RootConfigurationObject>(configJSON);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing configuration file: {0}", ex.Message);
            }

            foreach (var x in atmConfiguration.Subscriptions)
            {
                StorageAccounts.Instance.Add(x.PackageStorageAcct, x.PackageStorageKey, x.PackageContainerName);
                Subscriptions.Instance.Add(x);
            }

            var host = new JobHost();
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }

    }
}
