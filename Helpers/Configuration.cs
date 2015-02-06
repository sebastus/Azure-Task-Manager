using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Helpers
{    
    public class Configuration
    {
        public RootConfigurationObject atmConfiguration {get; set;}

        public void InitializeConfiguration()
        {
            var configString = Storage.GetConfigurationFile();

            try
            {
                atmConfiguration = JsonConvert.DeserializeObject<RootConfigurationObject>(configString);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing configuration file: {0}", ex.Message);
            }
        }

        public JSONSubscription GetSubscription(string subscriptionName)
        {
            foreach (var sub in atmConfiguration.Subscriptions)
            {
                if (sub.SubscriptionName == subscriptionName)
                    return sub;
            }
            return null;
        }
    }
}
