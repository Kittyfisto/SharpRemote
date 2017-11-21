using System.Net;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	/// </summary>
	[DataContract]
	[SerializationSurrogateFor(typeof(IPAddress))]
	internal sealed class IPAddressSurrogate
	{
	}
}