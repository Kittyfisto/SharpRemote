using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public class SystemException
        : Exception
    {
        public SystemException()
        {

        }

        public SystemException(string message, Exception innerException = null)
            : base(message, innerException)
        {

        }
    }
}
