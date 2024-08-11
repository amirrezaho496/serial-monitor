using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SerialM.Endpoint.WPF
{
    public class TextboxItem
    {
        public Run TimeRun { get; set; }
        public Run TextRun { get; set; }
        public string ResourceKey { get; set; } = string.Empty;

    }
}
