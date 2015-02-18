# Azure-Task-Manager
Microsoft Azure Task Manager, executes tasks on your Azure assets singly or in bulk.  

Core use cases: 
*	spin up and tear down large environments very quickly.
*	same techniques apply to test rigs (test agents)
*	even small environments can benefit from scheduled deploy/tear down

It costs roughly $60/month (at retail) to operate a single core VM 24x7 on Microsoft Azure.  If you have (like one of my customers) 50 interlocking web roles and dev/test/qa environments, the monthly spend piles up pretty quickly.  If some of those cores are only used 8 hours per day, why not shut them down?  It's a trade off between the cost of the time to start them up and shut them down every day and the convenience & cost of just leaving them running all night.

There is a way to capture both convenience and lower cost.  Through the use of automation, your services and VMs can be started and stopped automatically to meet your work schedule.  This tool, Azure Task Manager (ATM), offers these benefits.  

Set Up:

The system is implemented as an Azure Webjob.  Webjobs requires a storage account for its own needs, plus the dashboard.  ATM also requires a storage account for a few items.

*	Configuration of subscriptions & default storage account per subscription (JSON)
*	Management certificates for managed subscriptions (one each, PFX format with private key)

Open the solution in Visual Studio 2013, Update 4.  Azure SDK 2.5.  Deploy as Webjob.  Ensure that your webjob storage account is configured properly in the website connection strings section.  If it is not, you'll get a warning in the webjob dashboard.

Here is a sample configuration file:

{ 
    "Subscriptions": [
        {
            "SubscriptionName": "subscriptionName",
            "SubscriptionId": "subscriptionId",
            "MgtCertFileName": "CertFileName.pfx",
			"MgtCertPassword": "certPassword",
			"PackageStorageAcct":"storageAcctName",
			"PackageStorageKey":"reallyLongStorageAcctKey",
			"PackageContainerName":"atmpackages"
        }
    ]
}

Functions available in ATM:
*	CreateDeployment (cloud service/webrole/worker role)
*	CreateDeployment (VM)
*	ShutdownRole  (shutdown a VM)
*	DeleteDeployment (delete production or staging deployment of web/worker, any VMs)
*	DeleteService (delete a cloud service, confirm to delete if any deployments present)

Operation:

ATM works by accepting Azure storage queue messages.  Each message initiates one unit of work.  Messages are handled in parallel according to the Webjob operational norm.  To get the scheduled behavior, simply set up messages to be sent at the right time of day.  Message samples are in the repo.

Additional capabilities are being added.  Please post a comment if there are important use cases not currently covered.
