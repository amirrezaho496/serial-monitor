using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Business.Exceptions
{
    [System.Serializable]
    public class NotCompletedTaskException : Exception
    {
        public NotCompletedTaskException() { }
        public NotCompletedTaskException(string message) : base(message) { }
        public NotCompletedTaskException(string message, Exception inner) : base(message, inner) { }
        protected NotCompletedTaskException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
