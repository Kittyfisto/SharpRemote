using System;
using System.Runtime.Serialization;

namespace SharpRemote.Exceptions
{
	/// <summary>
	/// This exception is thrown when an application was referenced (by name) that doesn't exist.
	/// </summary>
	public sealed class NoSuchApplicationException
		: ArgumentException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public NoSuchApplicationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			ApplicationName = info.GetString("ApplicationName");
		}

		/// <inheritdoc />
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("ApplicationName", ApplicationName);
		}
#endif
#endif
		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public NoSuchApplicationException()
		{
			
		}

		/// <summary>
		/// Initializes a new instance of this exception with the application name in question and the
		/// inner exception, that was the cause of this exception.
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="e"></param>
		public NoSuchApplicationException(string applicationName, Exception e = null)
			: base(string.Format("There is no installed application with this name: {0}", applicationName), e)
		{
			ApplicationName = applicationName;
		}

		/// <summary>
		/// The name of the application in question.
		/// </summary>
		public readonly string ApplicationName;
	}
}