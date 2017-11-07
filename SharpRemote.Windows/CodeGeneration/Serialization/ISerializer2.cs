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
	///     Shall replace <see cref="ISerializer" />.
	/// </remarks>
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
		///     to the given stream.
		/// </summary>
		/// <remarks>
		///     It is expected that <see cref="CreateMethodInvocationReader" /> can read the contents of the stream
		///     written to by this method.
		/// </remarks>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be written to any more after
		///     the returned writer has been disposed of.
		/// </remarks>
		/// <param name="stream"></param>
		/// <param name="grainId"></param>
		/// <param name="methodName"></param>
		/// <param name="rpcId"></param>
		/// <returns></returns>
		IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream, ulong grainId, string methodName, ulong rpcId);

		/// <summary>
		/// </summary>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be read from any more after
		///     the returned reader has been disposed of.
		/// </remarks>
		/// <remarks>
		///     It is expected that this method can read a stream written to by <see cref="CreateMethodInvocationWriter" />.
		/// </remarks>
		/// <param name="stream">The stream from which the details of the method invocation are read from</param>
		/// <returns></returns>
		IMethodInvocationReader CreateMethodInvocationReader(Stream stream);

		/// <summary>
		///     Creates a writer which writes the result (or exception) foa method invocation to the given
		///     <paramref name="stream" />.
		/// </summary>
		/// <remarks>
		///     It is expected that <see cref="CreateMethodResultReader" /> can read the contents of the stream
		///     written to by this method.
		/// </remarks>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be written to any more after
		///     the returned writer has been disposed of.
		/// </remarks>
		/// <param name="stream">The stream to which the result (or exception) of the method invocation are written to</param>
		/// <param name="rpcId"></param>
		/// <returns></returns>
		IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId);

		/// <summary>
		///     Creates a reader which consumes the given stream and reads the result of a method invocation from it.
		/// </summary>
		/// <remarks>
		///     It is expected that this method can read a stream written to by <see cref="CreateMethodResultWriter" />.
		/// </remarks>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be read from any more after
		///     the returned reader has been disposed of.
		/// </remarks>
		/// <param name="stream">The stream from which the result of the method invocation is read from</param>
		/// <returns></returns>
		IMethodResultReader CreateMethodResultReader(Stream stream);
	}
}