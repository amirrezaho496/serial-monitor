using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Business.Exceptions
{
    public class AutoSendAlreadyRunningException : Exception
    {
        public AutoSendAlreadyRunningException()
            : base("Auto send is already running.")
        {
        }

        public AutoSendAlreadyRunningException(string message)
            : base(message)
        {
        }

        public AutoSendAlreadyRunningException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
