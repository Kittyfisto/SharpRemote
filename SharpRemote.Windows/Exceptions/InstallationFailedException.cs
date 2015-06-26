using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// Is thrown in case an installation failed.
	/// </summary>
	public class InstallationFailedException
		: SystemException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		public InstallationFailedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
#endif
#endif
		public InstallationFailedException()
		{
			
		}

		public InstallationFailedException(string message, Exception inner = null)
			: base(message, inner)
		{}
	}
}