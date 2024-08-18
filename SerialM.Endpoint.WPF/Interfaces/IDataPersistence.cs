using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Endpoint.WPF.Interfaces
{
    public interface IDataPersistence
    {
        void SaveText();
        void LoadText();
        void SaveConfig();
        void LoadConfig();
        void SaveAutoSendItems();
        void LoadAutoSendItems();
    }

}
