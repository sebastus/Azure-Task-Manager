using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Helpers
{
    public class StorageAccount
    {
        public StorageCredentials Credentials { get; set; }
        public string DefaultContainer { get; set; }
    }

    public class StorageAccounts
    {
        private Dictionary<string, StorageAccount> storageAccounts = new Dictionary<string,StorageAccount>();

        private static readonly StorageAccounts instance = new StorageAccounts();
        private StorageAccounts() 
        {
            CloudStorageAccount acct = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);
            storageAccounts.Add("config", new StorageAccount { Credentials = acct.Credentials, DefaultContainer = "atmconfiguration" });
        }

        static StorageAccounts() { }
        public static StorageAccounts Instance
        {
            get
            {
                return instance;
            }
        }

        public void Add(string accountName, string accountKey, string defaultContainer)
        {
            storageAccounts.Add(accountName, new StorageAccount{ 
                Credentials = new StorageCredentials(accountName, accountKey), 
                DefaultContainer = defaultContainer
            });
        }

        public StorageAccount Get(string accountName)
        {
            return storageAccounts[accountName];
        }
    }

    public class Storage
    {

        public Storage() 
        {

        }

        public string GetConfigurationFile()
        {
            string text;

            try
            {
                CloudStorageAccount acct = new CloudStorageAccount(StorageAccounts.Instance.Get("config").Credentials, true);
                CloudBlobClient cbc = acct.CreateCloudBlobClient();
                CloudBlobContainer container = cbc.GetContainerReference(StorageAccounts.Instance.Get("config").DefaultContainer);
                CloudBlockBlob blob = container.GetBlockBlobReference("config.json");
                text = blob.DownloadText();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting configuration file: {0}", ex.Message);
                return "";
            }

            Console.WriteLine("Configuration file fetched ok.");
            return text;
            
        }

        public byte[] GetBlockBlobBytes(string blobName)
        {
            byte[] blobBytes;

            try
            {
                CloudStorageAccount acct = new CloudStorageAccount(StorageAccounts.Instance.Get("config").Credentials, true);
                CloudBlobClient cbc = acct.CreateCloudBlobClient();
                CloudBlobContainer container = cbc.GetContainerReference(StorageAccounts.Instance.Get("config").DefaultContainer);
                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

                using (var memoryStream = new MemoryStream())
                {
                    blob.DownloadToStream(memoryStream);
                    blobBytes = memoryStream.ToArray();
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Error getting certificate file: {0}", ex.Message);
                return null;
            }

            Console.WriteLine("Certificate file fetched ok.");
            return blobBytes;
        }

        public bool BlobExists(string acctName, string blobName)
        {
            try
            {
                CloudStorageAccount acct = new CloudStorageAccount(StorageAccounts.Instance.Get(acctName).Credentials, true);
                CloudBlobClient cbc = acct.CreateCloudBlobClient();
                CloudBlobContainer container = cbc.GetContainerReference(StorageAccounts.Instance.Get(acctName).DefaultContainer);
                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
                bool blobExists = blob.Exists();
                return blobExists;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking existence of blob {1}: {0}", ex.Message, blobName);
                return false;
            }

            
        }

        public string GetServiceConfigFile(string acctName, string blobName)
        {
            CloudBlockBlob c;
            try
            {
                CloudStorageAccount acct = new CloudStorageAccount(StorageAccounts.Instance.Get(acctName).Credentials, true);
                CloudBlobClient cbc = acct.CreateCloudBlobClient();
                CloudBlobContainer container = cbc.GetContainerReference(StorageAccounts.Instance.Get(acctName).DefaultContainer);
                c = container.GetBlockBlobReference(blobName);
            }
            catch (StorageException ex)
            {
                Console.WriteLine("Error referencing config file blob: {0}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error referencing config file blob: {0}", ex.Message);
                return null;
            }

            byte[] configFileBytes = null;
            using (MemoryStream m = new MemoryStream())
            {
                c.DownloadToStream(m);
                configFileBytes = m.ToArray();
            }

            if (configFileBytes[0] == 0xEF)
            {
                Array.Copy(configFileBytes, 3, configFileBytes, 0, configFileBytes.Length - 3);
                configFileBytes[configFileBytes.Length - 1] = 0x0D;
                configFileBytes[configFileBytes.Length - 2] = 0x0D;
                configFileBytes[configFileBytes.Length - 3] = 0x0D;
            }


            return System.Text.Encoding.Default.GetString(configFileBytes);

        }

    }
}
