using System;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace SharpRemote.WebApi.Routes
{
	internal abstract class ArgumentParser
	{
		[Pure]
		public abstract object Extract(string str, int start, out int consumed);

		public static ArgumentParser Create(ParameterInfo parameter)
		{
			var type = parameter.ParameterType;
			if (type == typeof(Int32))
				return new Int32Parser();
			throw new ArgumentException(String.Format("Unable to find a parser for '{0}'", type));
		}
	}
}