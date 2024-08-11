using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Endpoint.WPF.Interfaces
{
    public interface ISaveable
    {
        string Path { get;}
        void Save();
        void Load();
    }
}
