using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a method is called that assumes that the endpoint is connected,
	/// but it was not.
	/// </summary>
	[Serializable]
	public class NotConnectedException
		: InvalidOperationException
	{
		/// <summary>
		/// The name of the endpoint that was not connected.
		/// </summary>
		public readonly string EndPointName;

#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserilization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public NotConnectedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			EndPointName = info.GetString("EndPointName");
		}

		/// <inheritdoc />
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("EndPointName", EndPointName);
		}
#endif
#endif

		/// <summary>
		/// Initializes a new instance of this exception with the given endpoint name.
		/// </summary>
		/// <param name="endPointName"></param>
		public NotConnectedException(string endPointName)
			: base("This endpoint is not connected to any other endpoint")
		{
			EndPointName = endPointName;
		}

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public NotConnectedException()
		{

		}
	}
}