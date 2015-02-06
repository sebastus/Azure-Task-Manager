using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public class Subscription
    {
        public Subscription(JSONSubscription json)
        {
            SubscriptionId = json.SubscriptionId;
            MgtCertFileName = json.MgtCertFileName;
            PackageStorageAcct = json.PackageStorageAcct;
            PackageStorageKey = json.PackageStorageKey;
            PackageContainerName = json.PackageContainerName;
        }

        public string SubscriptionId { get; set; }
        public string MgtCertFileName { get; set; }
        public string PackageStorageAcct { get; set; }
        public string PackageStorageKey { get; set; }
        public string PackageContainerName { get; set; }
        public X509Certificate2 MgtCertificate { get; set; }
    }

    public sealed class Subscriptions
    {
        private Dictionary<string, Subscription> subscriptions = new Dictionary<string,Subscription>();

        private static readonly Subscriptions instance = new Subscriptions();
        private Subscriptions() { }
        static Subscriptions() { }
        public static Subscriptions Instance
        {
            get
            {
                return instance;
            }
        }

        public void Add(JSONSubscription json) 
        {
            subscriptions.Add(json.SubscriptionName, new Subscription(json));
            byte[] certBytes = Storage.GetBlockBlobBytes(json.MgtCertFileName);
            X509Certificate2 cert = new X509Certificate2(certBytes, json.MgtCertPassword, X509KeyStorageFlags.MachineKeySet);
            subscriptions[json.SubscriptionName].MgtCertificate = cert;
        }

        public Subscription Get(string subName)
        {
            return subscriptions[subName];
        }
    }
}
