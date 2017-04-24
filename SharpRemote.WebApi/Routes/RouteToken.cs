using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace SharpRemote.WebApi.Routes
{
	internal struct RouteToken : IEquatable<RouteToken>
	{
		public readonly TokenType Type;
		public readonly string Pattern;

		public bool Equals(RouteToken other)
		{
			return Type == other.Type && string.Equals(Pattern, other.Pattern) && ArgumentIndex == other.ArgumentIndex;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is RouteToken && Equals((RouteToken) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int) Type;
				hashCode = (hashCode * 397) ^ (Pattern != null ? Pattern.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ ArgumentIndex;
				return hashCode;
			}
		}

		public static bool operator ==(RouteToken left, RouteToken right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(RouteToken left, RouteToken right)
		{
			return !left.Equals(right);
		}

		public readonly int ArgumentIndex;

		public RouteToken(TokenType type, string pattern, int index)
		{
			Type = type;
			Pattern = pattern;
			ArgumentIndex = index;
		}

		public enum TokenType
		{
			Constant,
			Argument
		}

		public static IEnumerable<RouteToken> Tokenize(string pattern)
		{
			var tokens = new List<RouteToken>();
			if (pattern != null)
			{
				int startIndex = 0;
				int index;
				while ((index = pattern.IndexOf("{", startIndex, StringComparison.Ordinal)) != -1)
				{
					if (startIndex != index)
						tokens.Add(Constant(pattern.Substring(startIndex, index - startIndex)));

					startIndex = index;
					index = pattern.IndexOf("}", startIndex, StringComparison.Ordinal);
					if (index == -1)
						throw new ArgumentException();

					var argumentIndexStr = pattern.Substring(startIndex + 1, index - startIndex - 1);
					int argumentIndex;
					if (!int.TryParse(argumentIndexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out argumentIndex))
						throw new ArgumentException();

					if (argumentIndex < 0)
						throw new ArgumentException();

					tokens.Add(Argument(argumentIndex));

					startIndex = index + 1;
				}
				if (startIndex < pattern.Length)
					tokens.Add(Constant(pattern.Substring(startIndex)));
			}
			return tokens;
		}

		[Pure]
		public static RouteToken Argument(int argumentIndex)
		{
			return new RouteToken(TokenType.Argument, null, argumentIndex);
		}

		[Pure]
		public static RouteToken Constant(string substring)
		{
			return new RouteToken(TokenType.Constant, substring, 0);
		}
	}
}