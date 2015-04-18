using System;
using System.Net;

namespace SharpRemote
{
	/// <summary>
	/// Represents an endpoint in another AppDomain, Process or Machine.
	/// </summary>
	public interface IEndPoint
		: IDisposable
	{
		IPEndPoint Address { get; }
	}
}