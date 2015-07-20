using System;
using System.Linq;
using System.Reflection;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	internal static class TypeExtensions
	{
#if !WINDOWS_PHONE_APP
		public static bool Is(this Type that, Type type)
		{
			if (that == null) throw new NullReferenceException();

			if (that.GetInterfaces().Any(x => x == type))
				return true;

			while (that != typeof(object) && that != null)
			{
				if (that == type)
					return true;

				that = that.BaseType;
			}

			return type == typeof(object);
		}

		/// <summary>
		/// Retrieves the first custom attribute type <paramref name="T"/> from the given
		/// type, including its base classes and interfaces.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="inherited"></param>
		/// <returns></returns>
		public static T GetRealCustomAttribute<T>(this Type type, bool inherited)
			where T : Attribute
		{
			var attribute = type.GetCustomAttribute<T>(inherited);
			if (attribute != null)
				return attribute;

			if (inherited)
			{
				foreach (var iface in type.GetInterfaces())
				{
					attribute = iface.GetCustomAttribute<T>();
					if (attribute != null)
						return attribute;
				}
			}

			return null;
		}
#endif
	}
}