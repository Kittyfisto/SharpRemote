using System.IO;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     The interface a serializer needs to implement:
	///     An RPC call is modelled by the following sequence of methods:
	///     - <see cref="CreateMethodInvocationWriter" /> [Caller]
	///     - <see cref="CreateMethodInvocationReader" /> [Callee]
	///     - <see cref="CreateMethodResultWriter" /> [Callee]
	///     - <see cref="CreateMethodResultReader" /> [Caller]
	///     Where [Caller] is the serializer object on the side of the caller and [Callee] is the
	///     serializer object on the side of the callee of the method.
	/// </summary>
	/// <remarks>
	///     This interface is responsible for deciding if a type model from another endpoint can be accepted
	///     or not (because serializers have the freedom to decide how [or if at all] compatible their messages
	///     are with regards to changes to the type model: One serializer might not care if a property has been
	///     added while another might care).
	/// </remarks>
	public interface ISerializer2
	{
		/// <summary>
		///     Creates a new writer which writes the intention to call the given method on the given object
		///     to the given stream. The stream should be fully written to once the given writer is disposed of.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="grainId"></param>
		/// <param name="methodName"></param>
		/// <param name="rpcId"></param>
		/// <returns></returns>
		IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream, ulong grainId, string methodName, ulong rpcId);

		/// <summary>
		/// </summary>
		/// <param name="stream">The stream from which the details of the method invocation are read from</param>
		/// <returns></returns>
		IMethodInvocationReader CreateMethodInvocationReader(Stream stream);

		/// <summary>
		/// </summary>
		/// <param name="stream">The stream to which the result (or exception) of the method invocation are written to</param>
		/// <param name="grainId"></param>
		/// <param name="methodName"></param>
		/// <param name="rpcId"></param>
		/// <returns></returns>
		IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong grainId, string methodName, ulong rpcId);

		/// <summary>
		/// </summary>
		/// <param name="stream">The stream from which the result of the method invocation is read from</param>
		/// <returns></returns>
		IMethodResultReader CreateMethodResultReader(Stream stream);
	}
}