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

        public static void ProcessStartVMsMessage([QueueTrigger("startvms")] string message, TextWriter log)
        {
            StartRolesProcess.StartRolesSteps(message, log);
        }

        public static void ProcessShutdownVMsMessage([QueueTrigger("shutdownvms")] string message, TextWriter log)
        {
            ShutdownVMsProcess.ShutdownVMsSteps(message, log);
        }

        public static void ProcessDeleteDeploymentMessage([QueueTrigger("deletedeployment")] string message, TextWriter log)
        {
            DeleteDeploymentProcess.DeleteDeploymentSteps(message, log);
        }

        public static void ProcessDeleteServiceMessage([QueueTrigger("deleteservice")] string message, TextWriter log)
        {
            DeleteServiceProcess.DeleteServiceSteps(message, log);
        }

    }
}
