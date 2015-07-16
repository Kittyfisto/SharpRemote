using System;
using System.Collections.Generic;

namespace SharpRemote.Hosting
{
	internal sealed class DefaultImplementationRegistry
	{
		private readonly Dictionary<Type, Type> _types;

		public DefaultImplementationRegistry()
		{
			_types = new Dictionary<Type, Type>();
		}

		public void RegisterDefaultImplementation(Type implementation, Type interfaceType)
		{
			lock (_types)
			{
				if (_types.ContainsKey(interfaceType))
					throw new ArgumentException(
						string.Format("There already is a default implementation for interface type '{0}' defined",
						interfaceType
						));

				_types.Add(interfaceType, implementation);
			}
		}

		public Type GetImplementation(Type interfaceType)
		{
			lock (_types)
			{
				Type implementation;
				if (!_types.TryGetValue(interfaceType, out implementation))
					throw new ArgumentException(string.Format("There is no default implementation for interface type '{0}' defined", interfaceType));

				return implementation;
			}
		}
	}
}