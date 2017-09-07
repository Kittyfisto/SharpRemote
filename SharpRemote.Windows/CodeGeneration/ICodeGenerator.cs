using System;

namespace SharpRemote.CodeGeneration
{
	/// <summary>
	/// 
	/// </summary>
	public interface ICodeGenerator
	{
		#region Proxies

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		Type GenerateServant<T>();

		/// <summary>
		/// 
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
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		Type GenerateProxy<T>();

		/// <summary>
		/// 
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