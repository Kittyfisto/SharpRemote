using System;
using SharpRemote.WebApi.Requests;

namespace SharpRemote.WebApi
{
	/// <summary>
	///     Allows access to one or more user-defined-resources.
	/// </summary>
	public interface IWebApiController
		: IRequestHandler
		, IDisposable
	{
		/// <summary>
		///     Registers a new resource with this endpoint.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name of the resource</param>
		/// <param name="controller"></param>
		/// <returns>The complete uri under which the newly registered resource can be queried</returns>
		void AddResource<T>(string name, T controller);
	}
}