using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     The interface a serializer needs to implement:
	///     An RPC call is modelled by the following sequence of methods:
	///     - <see cref="CreateMethodInvocationWriter" /> [Caller]
	///     - <see cref="CreateMethodReader" /> [Callee]
	///     - <see cref="CreateMethodResultWriter" /> [Callee]
	///     - <see cref="CreateMethodReader" /> [Caller]
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
		///     Registers the given type <typeparamref name="T" /> with this serializer.
		/// </summary>
		/// <remarks>
		///     This method can be used to verify upfront that:
		///     - A used type can be serialized
		///     - No intermittent compilation happens while writing / reading an object graph
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <exception cref="ArgumentNullException">When <typeparamref name="T" /> is null</exception>
		/// <exception cref="ArgumentException">When the type cannot be serialized</exception>
		/// <exception cref="SerializationException">
		///     In case there was an error while generating the code necessary for
		///     serialization / deserialization
		/// </exception>
		void RegisterType<T>();

		/// <summary>
		///     Registers the given type <paramref name="type" /> with this serializer.
		/// </summary>
		/// <remarks>
		///     This method can be used to verify upfront that:
		///     - A used type can be serialized
		///     - No intermittent compilation happens while writing / reading an object graph
		/// </remarks>
		/// <param name="type">The type to register</param>
		/// <exception cref="ArgumentNullException">When <paramref name="type" /> is null</exception>
		/// <exception cref="ArgumentException">When the type cannot be serialized</exception>
		/// <exception cref="SerializationException">
		///     In case there was an error while generating the code necessary for
		///     serialization / deserialization
		/// </exception>
		void RegisterType(Type type);

		/// <summary>
		///     Tests if the given type <typeparamref name="T" /> has already been registered
		///     with this serializer (either directly through <see cref="RegisterType" /> or indirectly
		///     through <see cref="CreateMethodInvocationWriter" />, <see cref="CreateMethodReader" /> or
		///     <see cref="CreateMethodResultWriter" />.
		/// </summary>
		/// <typeparam name="T">The type to test</typeparam>
		/// <returns>True if the type has been registered, false otherwise</returns>
		[Pure]
		bool IsTypeRegistered<T>();

		/// <summary>
		///     Tests if the given type <paramref name="type" /> has already been registered
		///     with this serializer (either directly through <see cref="RegisterType" /> or indirectly
		///     through <see cref="CreateMethodInvocationWriter" />, <see cref="CreateMethodResultWriter" /> or
		///     <see cref="CreateMethodReader" />.
		/// </summary>
		/// <param name="type">The type to test</param>
		/// <returns>True if the type has been registered, false otherwise</returns>
		[Pure]
		bool IsTypeRegistered(Type type);

		/// <summary>
		///     Creates a new writer which writes the intention to call the given method on the given object
		///     to the given stream.
		/// </summary>
		/// <remarks>
		///     It is expected that <see cref="CreateMethodReader" /> can read the contents of the stream
		///     written to by this method.
		/// </remarks>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be written to any more after
		///     the returned writer has been disposed of.
		/// </remarks>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be disposed of when the returned
		///     <see cref="IMethodInvocationWriter" /> is disposed of:
		///     That writer does NOT own the stream and should NOT close it either.
		/// </remarks>
		/// <param name="stream"></param>
		/// <param name="rpcId"></param>
		/// <param name="grainId"></param>
		/// <param name="methodName"></param>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		IMethodInvocationWriter CreateMethodInvocationWriter(Stream stream,
		                                                     ulong rpcId,
		                                                     ulong grainId,
		                                                     string methodName,
		                                                     IRemotingEndPoint endPoint = null);

		/// <summary>
		///     Creates a writer which writes the result (or exception) foa method invocation to the given
		///     <paramref name="stream" />.
		/// </summary>
		/// <remarks>
		///     It is expected that <see cref="CreateMethodReader" /> can read the contents of the stream
		///     written to by this method.
		/// </remarks>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be written to any more after
		///     the returned writer has been disposed of.
		/// </remarks>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be disposed of when the returned
		///     <see cref="IMethodResultWriter" /> is disposed of:
		///     That writer does NOT own the stream and should NOT close it either.
		/// </remarks>
		/// <param name="stream">The stream to which the result (or exception) of the method invocation are written to</param>
		/// <param name="rpcId"></param>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		IMethodResultWriter CreateMethodResultWriter(Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null);

		/// <summary>
		///     Creates a reader which consumes the given stream and either creates a <see cref="IMethodInvocationReader" />
		///     if the stream was written by a compatible <see cref="IMethodInvocationWriter" /> or a
		///     <see cref="IMethodResultReader" /> if it was written by a <see cref="IMethodResultWriter" />.
		/// </summary>
		/// <remarks>
		///     The given <paramref name="stream" /> should NOT be disposed of when the returned
		///     <see cref="IMethodResultReader" /> is disposed of:
		///     That reader does NOT own the stream and should NOT close it either.
		/// </remarks>
		/// <param name="stream">The stream from which the result of the method invocation is read from</param>
		/// <param name="resultReader"></param>
		/// <param name="endPoint"></param>
		/// <param name="invocationReader"></param>
		/// <returns></returns>
		void CreateMethodReader(Stream stream,
		                        out IMethodInvocationReader invocationReader,
		                        out IMethodResultReader resultReader,
		                        IRemotingEndPoint endPoint = null);
	}
}