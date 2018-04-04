using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// Is thrown in case an installation failed.
	/// </summary>
	[Serializable]
	public class InstallationFailedException
		: SystemException
	{
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public InstallationFailedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public InstallationFailedException()
		{
			
		}

		/// <summary>
		/// Initializes a new instance of this exception with the given message and inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="inner"></param>
		public InstallationFailedException(string message, Exception inner = null)
			: base(message, inner)
		{}
	}
}