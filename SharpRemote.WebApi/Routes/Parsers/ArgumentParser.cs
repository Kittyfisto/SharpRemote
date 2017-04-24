using System;
using System.Diagnostics.Contracts;

namespace SharpRemote.WebApi.Routes.Parsers
{
	internal abstract class ArgumentParser
	{
		[Pure]
		public abstract bool TryExtract(string str,
			int startIndex,
			out object value,
			out int consumed);

		public static ArgumentParser Create(Type parameterType)
		{
			var type = parameterType;
			if (type == typeof(bool))
				return new BoolParser();
			if (type == typeof(string))
				return new StringParser();
			if (type == typeof(sbyte))
				return new SByteParser();
			if (type == typeof(byte))
				return new ByteParser();
			if (type == typeof(Int16))
				return new Int16Parser();
			if (type == typeof(UInt16))
				return new UInt16Parser();
			if (type == typeof(Int32))
				return new Int32Parser();
			if (type == typeof(UInt32))
				return new UInt32Parser();
			if (type == typeof(Int64))
				return new Int64Parser();
			if (type == typeof(UInt64))
				return new UInt64Parser();
			throw new ArgumentException(String.Format("Unable to find a parser for '{0}'", type));
		}
	}
}