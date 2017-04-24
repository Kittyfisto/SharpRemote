using System;
using System.Collections.Generic;
using System.Linq;
using SharpRemote.WebApi.Routes.Parsers;

namespace SharpRemote.WebApi.Routes
{
	internal sealed class Route
	{
		private readonly List<RouteToken> _tokens;
		private readonly List<ArgumentParser> _arguments;
		private readonly HttpMethod _method;

		public HttpMethod Method => _method;

		public Route(HttpMethod method, string route, IEnumerable<Type> parameterTypes)
		{
			_tokens = RouteToken.Tokenize(route);
			_arguments = new List<ArgumentParser>(parameterTypes.Select(ArgumentParser.Create));
			_method = method;

			var arguments = _tokens.Count(x => x.Type == RouteToken.TokenType.Argument);
			if (arguments != _arguments.Count)
				throw new ArgumentException();

			foreach (var token in _tokens)
			{
				if (token.Type == RouteToken.TokenType.Argument)
				{
					if (token.ArgumentIndex >= _arguments.Count ||
					    token.ArgumentIndex < 0)
						throw new ArgumentOutOfRangeException(string.Format(
							"Referencing non-existant argument #{0} (there are only {1} arguments)",
							token.ArgumentIndex,
							_arguments.Count));
				}
			}
		}

		public bool TryMatch(string route, out object[] values)
		{
			var arguments = new object[_arguments.Count];
			int numArguments = 0;
			int startIndex = 0;
			for(int i = 0; i < _tokens.Count; ++i)
			{
				var token = _tokens[i];
				if (token.Type == RouteToken.TokenType.Argument)
				{
					var argument = _arguments[token.ArgumentIndex];
					int consumed;
					object value;
					if (!argument.TryExtract(route, startIndex, out value, out consumed))
						break;

					arguments[token.ArgumentIndex] = value;
					startIndex += consumed;
					++numArguments;
				}
				else
				{
					if (route.IndexOf(token.Pattern, startIndex) != startIndex)
						break;

					startIndex += token.Pattern.Length;
				}
			}

			if (startIndex < route.Length)
			{
				values = null;
				return false;
			}

			if (numArguments != arguments.Length)
			{
				values = null;
				return false;
			}

			values = arguments;
			return true;
		}
	}
}