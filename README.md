# Azure-Task-Manager
Microsoft Azure Task Manager, executes tasks on your Azure assets singly or in bulk.  

Core use cases: 
*	spin up and tear down large environments very quickly.
*	same techniques apply to test rigs (test agents)
*	even small environments can benefit from scheduled deploy/tear down

It costs roughly $60/month (at retail) to operate a single core VM 24x7 on Microsoft Azure.  If you have (like one of my customers) 50 interlocking web roles and dev/test/qa environments, the monthly spend piles up pretty quickly.  If some of those cores are only used 8 hours per day, why not shut them down?  It's a trade off between the cost of the time to start them up and shut them down every day and the convenience & cost of just leaving them running all night.

There is a way to capture both convenience and lower cost.  Through the use of automation, your services and VMs can be started and stopped automatically to match your work schedule.  This tool, Azure Task Manager (ATM), offers these benefits.  

Set Up:

The system is implemented as an Azure Webjob.  Webjobs are contained or hosted within an Azure Website.  Create an Azure Website.

Webjobs (in general) require a storage account for their own needs (referred to as AzureWebJobsStorage in the app.config), plus one for the dashboard data (referred to as AzureWebJobsDashboard in app.config).  Create a storage account for each and put the connection strings into app.config in the designated locations.  In the configuration page of the Azure Website that will host the webjob, add Connection Strings - one for each storage account.

ATM also requires a storage account for a few items.  These items go into the storage account referred to as AzureWebJobsStorage in app.config.  You must create a container named 'atmconfiguration' in the blob storage of that storage account.  Into that container place the following files:

*	Configuration of subscriptions & default storage account per subscription (config.json)
*	Management certificates for managed subscriptions (one each, PFX format with private key, for example myssl.pfx)

In that same storage account, create a set of Azure Storage Queues:
*	createdeployment
*	createvm
*	startvms
*	shutdownvms
*	deletedeployment
*	deleteservice

Open the solution in Visual Studio 2013, Update 4.  Azure SDK 2.5.  Deploy as Webjob.

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
*	StartRole (start an existing VM)
*	ShutdownRole  (shutdown a VM)
*	DeleteDeployment (delete production or staging deployment of web/worker, any VMs)
*	DeleteService (delete a cloud service, confirm to delete if any deployments present)

Operation:

ATM works by accepting Azure storage queue messages.  Each message initiates one unit of work.  Messages are handled in parallel according to the Webjob operational norm.  To get the scheduled behavior, simply set up messages to be sent at the right time of day.  Or hook the message transmission to your continuous integration mechanism.  Message samples are in the repo.  Look for files with "json" extension.

Additional capabilities are being added.  Please post a comment if there are important use cases not currently covered.
