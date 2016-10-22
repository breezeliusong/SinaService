using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace SinaService.SinaServiceHelper
{
    //provider static helper method for connections
    public class ConnectionHelper
    {
        /// <summary>
        /// Get a value indicating whether if the current internet connection is metered
        /// </summary>
        public static bool IsInternetOnMeterdConnection
        {
            get
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                return profile?.GetConnectionCost().NetworkCostType != NetworkCostType.Unrestricted;
            }
        }

        /// <summary>
        /// Gets a value indicating whether internet is available across all connections.
        /// </summary>
        /// <return>True if internet can be reached</return>
        public static bool IsInternetAvailable
        {
            get
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    return false;
                }
                return NetworkInformation.GetConnectionProfiles() != null;
            }
        }
    }
}
