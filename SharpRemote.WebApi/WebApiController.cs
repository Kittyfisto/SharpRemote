using System.Collections.Generic;
using SharpRemote.WebApi.Requests;
using SharpRemote.WebApi.Resources;

namespace SharpRemote.WebApi
{
	/// <summary>
	///     Allows access to one or more user-defined-resources via an http socket endpoint.
	/// </summary>
	public sealed class WebApiController
		: IWebApiController
	{
		private readonly object _syncRoot;
		private readonly Dictionary<string, IResource> _resourceControllers;

		/// <summary>
		/// </summary>
		public WebApiController()
		{
			_syncRoot = new object();
			_resourceControllers = new Dictionary<string, IResource>();
		}

		/// <inheritdoc />
		public void AddResource<T>(string name, T controller)
		{
			var handler = Resource.Create(controller);
			lock (_syncRoot)
			{
				_resourceControllers.Add(name, handler);
			}
		}

		/// <inheritdoc />
		public WebResponse TryHandle(string subUri, WebRequest request)
		{
			string resourceName;
			string resourceSubUri;
			if (ExtractResourceName(subUri, out resourceName, out resourceSubUri))
			{
				lock (_resourceControllers)
				{
					IResource resource;
					if (_resourceControllers.TryGetValue(resourceName, out resource))
					{
						var response = resource.TryHandleRequest(subUri, request);
						if (response != null)
						{
							return response;
						}
					}
				}
			}

			return new WebResponse(404);
		}

		private bool ExtractResourceName(string subUri, out string resourceName, out string resourceSubUri)
		{
			int idx = subUri.IndexOf("/");
			if (idx != -1)
			{
				resourceName = subUri.Substring(0, idx - 1);
				resourceSubUri = subUri.Substring(idx + 1);
				return true;
			}

			resourceName = null;
			resourceSubUri = null;
			return false;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			
		}
	}
}