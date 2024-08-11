using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Business.Extensions
{
    public static class PathExtentions
    {
        public static string SendSerialItemsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SendItemsData.json");
        public static string SerialConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SerialConfig.json");
        public static string LogsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs.json");
    }
}
