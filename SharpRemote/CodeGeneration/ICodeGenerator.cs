using System;

namespace SharpRemote.CodeGeneration
{
	/// <summary>
	///     The interface for a code generator that is responsible for providing proxy implementations of
	///     interfaces as well as servants which work in conjunction with a <see cref="IRemotingEndPoint" />
	///     and <see cref="IEndPointChannel" />.
	/// </summary>
	public interface ICodeGenerator
	{
		#region Proxies

		/// <summary>
		///     Provides the .NET type of the servant for the given interface.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		Type GenerateServant<T>();

		/// <summary>
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="channel"></param>
		/// <param name="objectId"></param>
		/// <param name="subject"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		IServant CreateServant<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId, T subject);

		#endregion

		#region Servants

		/// <summary>
		///     Provides the .NET type of the proxy for the given interface.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		Type GenerateProxy<T>();

		/// <summary>
		///     Creates a proxy object for the given interface.
		///     All method calls are forwarded to the given <see cref="IEndPointChannel" />.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="channel"></param>
		/// <param name="objectId"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		T CreateProxy<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId);

		#endregion
	}
}