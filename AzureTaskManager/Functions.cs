using Microsoft.Azure.WebJobs;
using System.IO;

namespace AzureTaskManager
{

    public class Functions
    {                
        
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessCreateDeploymentMessage([QueueTrigger("createdeployment")] string message, TextWriter log)
        {
            CreateDeploymentProcess.CreateDeploymentSteps(message, log);
        }
        
    }
}
