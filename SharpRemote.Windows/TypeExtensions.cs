using System;
using System.Linq;

namespace SharpRemote
{
	public static class TypeExtensions
	{
		public static bool Is(this Type that, Type type)
		{
			if (that == null) throw new NullReferenceException();

			if (that.GetInterfaces().Any(x => x == type))
				return true;

			while (that != typeof(object))
			{
				if (that == type)
					return true;

				that = that.BaseType;
			}

			return type == typeof(object);
		}
	}
}