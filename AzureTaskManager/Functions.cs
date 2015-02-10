using Microsoft.Azure.WebJobs;
using System.IO;

namespace AzureTaskManager
{

    public class Functions
    {                
        
        public static void ProcessCreateDeploymentMessage([QueueTrigger("createdeployment")] string message, TextWriter log)
        {
            CreateDeploymentProcess.CreateDeploymentSteps(message, log);
        }

        public static void ProcessCreateVMMessage([QueueTrigger("createvm")] string message, TextWriter log)
        {
            CreateVMProcess.CreateVMSteps(message, log);
        }

    }
}
