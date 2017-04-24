using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using log4net;
using SharpRemote.WebApi.Routes;

namespace SharpRemote.WebApi.Requests
{
	/// <summary>
	///     Represents a resource exposed via a web api.
	///     Responsible for forwarding web requests to the actual class that controls
	///     access to the specific resource.
	/// </summary>
	internal sealed class Resource
		: IResource
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Type _interfaceType;
		private readonly object _controller;
		private readonly Dictionary<Route, MethodInfo> _methods;

		public static Resource Create<T>(T controller)
		{
			return new Resource(typeof(T), controller);
		}

		private Resource(Type interfaceType, object controller)
		{
			if (interfaceType == null)
				throw new ArgumentNullException(nameof(interfaceType));
			if (controller == null)
				throw new ArgumentNullException(nameof(controller));

			_interfaceType = interfaceType;
			_controller = controller;
			_methods = new Dictionary<Route, MethodInfo>();

			var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
			foreach (var method in methods)
			{
				var route = ExtractRoute(method);
				if (route != null)
				{
					_methods.Add(route, method);
				}
			}
		}

		private Route ExtractRoute(MethodInfo method)
		{
			var attribute = method.GetCustomAttribute<HttpAttribute>();
			if (attribute == null)
				return null;

			var parameters = method.GetParameters();
			return new Route(attribute.Method, attribute.Route, parameters.Select(x => x.ParameterType));
		}

		public WebResponse TryHandleRequest(string uri, WebRequest request)
		{
			object[] arguments;
			var method = FindMethod(request.Method, uri, out arguments);
			if (method == null)
				return null;

			return Handle(request, method, arguments);
		}

		private MethodInfo FindMethod(HttpMethod httpMethod, string url, out object[] arguments)
		{
			foreach (var pair in _methods)
			{
				var route = pair.Key;
				var method = pair.Value;

				if (route.Method == httpMethod &&
				    route.TryMatch(url, out arguments))
				{
					return method;
				}
			}

			arguments = null;
			return null;
		}

		private WebResponse Handle(WebRequest request, MethodInfo method, object[] arguments)
		{
			WebResponse response;
			try
			{
				var ret = method.Invoke(_controller, arguments);
				var serializer = new JavaScriptSerializer();
				var builder = new StringBuilder();
				serializer.Serialize(ret, builder);
				response = new WebResponse(200, builder.ToString());
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception while handling '{0}': {1}", request.Url, e);
				response = new WebResponse(500);
			}
			return response;
		}
	}
}