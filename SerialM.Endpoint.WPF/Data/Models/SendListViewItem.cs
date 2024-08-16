using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Endpoint.WPF.Models
{
    public class SendListViewItem
    {
        public string Text { get; set; } = string.Empty;
        public bool CanSend { get; set; } = true;
        public int Delay { get; set; } = 0;

    }
}
