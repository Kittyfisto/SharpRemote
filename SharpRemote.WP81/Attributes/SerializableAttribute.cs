using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// Stub attribute to allow exceptions to compile...
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SerializableAttribute
        : Attribute
    {
    }
}
