using System.Collections.Generic;
using SharpRemote.WebApi.Requests;

namespace SharpRemote.WebApi
{
	/// <summary>
	///     Allows access to one or more user-defined-resources via an http socket endpoint.
	/// </summary>
	public sealed class WebApiController
		: IWebApiController
	{
		private readonly object _syncRoot;
		private readonly IRequestHandlerCreator _requestHandlerCreator;
		private readonly Dictionary<string, IRequestHandler> _resourceControllers;

		/// <summary>
		/// </summary>
		public WebApiController()
		{
			_syncRoot = new object();
			_requestHandlerCreator = new RequestHandlerCreator();
			_resourceControllers = new Dictionary<string, IRequestHandler>();
		}

		/// <inheritdoc />
		public void AddResource<T>(string name, T controller)
		{
			var handler = _requestHandlerCreator.Create(controller);
			lock (_syncRoot)
			{
				_resourceControllers.Add(name, handler);
			}
		}

		/// <inheritdoc />
		public WebResponse TryHandle(WebRequest request)
		{
			lock (_resourceControllers)
			{
				foreach (var handler in _resourceControllers.Values)
				{
					var response = handler.TryHandle(request);
					if (response != null)
					{
						return response;
					}
				}
			}

			return new WebResponse(404);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			
		}
	}
}