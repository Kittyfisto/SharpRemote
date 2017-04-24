using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpRemote.WebApi.Routes
{
	internal sealed class Route
	{
		private readonly List<ArgumentParser> _arguments;
		private readonly HttpMethod _method;
		private string _route;

		public Route(HttpAttribute attribute, ParameterInfo[] parameters)
		{
			_route = attribute.Route;
			_arguments = new List<ArgumentParser>(parameters.Select(ArgumentParser.Create));
			_method = attribute.Method;
		}

		public bool TryMatch(Uri url, out object[] values)
		{
			var arguments = new object[_arguments.Count];
			var uri = url.ToString();
			int start = 0;
			for(int i = 0; i < _arguments.Count; ++i)
			{
				var argument = _arguments[i];

				int consumed;
				var value = argument.Extract(uri, start, out consumed);
				if (value == null)
				{
					values = null;
					return false;
				}
			}

			values = arguments;
			return true;
		}
	}
}