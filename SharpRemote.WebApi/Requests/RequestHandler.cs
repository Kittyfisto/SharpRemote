using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using SharpRemote.WebApi.Routes;

namespace SharpRemote.WebApi.Requests
{
	internal sealed class RequestHandler
		: IRequestHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Type _type;
		private readonly object _controller;
		private readonly Dictionary<Route, MethodInfo> _methods;

		public RequestHandler(Type type, object controller)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (controller == null)
				throw new ArgumentNullException(nameof(controller));

			_type = type;
			_controller = controller;
			_methods = new Dictionary<Route, MethodInfo>();

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
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
			return new Route(attribute, method.GetParameters());
		}

		public WebResponse TryHandle(WebRequest request)
		{
			object[] arguments;
			var method = FindMethod(request.Url, out arguments);
			if (method == null)
				return null;

			return Handle(request, method, arguments);
		}

		private WebResponse Handle(WebRequest request, MethodInfo method, object[] arguments)
		{
			WebResponse response;
			try
			{
				var ret = method.Invoke(_controller, arguments);
				response = new WebResponse(200);
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception while handling '{0}': {1}", request.Url, e);
				response = new WebResponse(500);
			}
			return response;
		}

		private MethodInfo FindMethod(Uri url, out object[] arguments)
		{
			foreach (var pair in _methods)
			{
				var route = pair.Key;
				var method = pair.Value;

				if (route.TryMatch(url, out arguments))
				{
					return method;
				}
			}

			arguments = null;
			return null;
		}
	}
}