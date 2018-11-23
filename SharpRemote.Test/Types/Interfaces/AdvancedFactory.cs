using System;
using System.Collections.Generic;

namespace SharpRemote.Test.Types.Interfaces
{
	public sealed class AdvancedFactory
		: IAdvancedFactory
	{
		private readonly List<object> _values;

		public AdvancedFactory()
		{
			_values = new List<object>();
		}

		public object Create(Type type)
		{
			var value = Activator.CreateInstance(type);
			_values.Add(value);
			return value;
		}
	}
}