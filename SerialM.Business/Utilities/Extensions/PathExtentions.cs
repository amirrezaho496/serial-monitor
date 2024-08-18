using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Business.Utilities.Extensions
{
    public static class PathExtentions
    {
        public static string SendSerialItemsPath  => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SendItemsData.json");
        public static string SerialConfigPath     => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SerialConfig.json");
        public static string SerialLogsPath       => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs.json");
        public static string SendNetworkItemsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetworkSendItemsData.json");
        public static string NetworkConfigPath    => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetworkConfig.json");
        public static string NetworkLogsPath      => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetworkLogs.json");
    }
}
