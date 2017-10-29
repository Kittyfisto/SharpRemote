using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public partial class Serializer
	{
		private void EmitReadSingleton(ILGenerator gen, MethodInfo factoryMethod)
		{
			gen.Emit(OpCodes.Call, factoryMethod);
		}

		/// <inheritdoc />
		public void RegisterSingleton<T>(MethodInfo getSingleton)
		{
			if (getSingleton == null) throw new ArgumentNullException(nameof(getSingleton));
			if (!getSingleton.IsStatic) throw new ArgumentException("The factory method must be static", nameof(getSingleton));
			if (!getSingleton.IsPublic) throw new ArgumentException("The factory method must be publicly visible");

			_getSingletonInstance.Add(typeof(T), getSingleton);
			RegisterType<T>();
		}

		[Pure]
		private bool IsSingleton(Type type, out MethodInfo method)
		{
			if (!_getSingletonInstance.TryGetValue(type, out method))
			{
				var factories = type.GetMethods()
					.Where(x => x.GetCustomAttribute<SingletonFactoryMethodAttribute>() != null)
					.Concat(type.GetProperties()
						.Where(x => x.GetCustomAttribute<SingletonFactoryMethodAttribute>() != null)
						.Select(x => x.GetMethod)).ToList();

				if (factories.Count == 0)
					return false;

				if (factories.Count > 1)
					throw new ArgumentException(string.Format("The type '{0}' has more than one singleton factory - this is not allowed", type));

				method = factories[0];
				if (method.ReturnType != type)
					throw new ArgumentException(string.Format("The factory method '{0}.{1}' is required to return a value of type '{0}' but doesn't", type, method));

				var @interface = type.GetInterfaces().FirstOrDefault(x => x.GetCustomAttribute<ByReferenceAttribute>() != null);
				if (@interface != null)
				{
					throw new ArgumentException(string.Format(
						"The type '{0}' both has a method marked with the SingletonFactoryMethod attribute and also implements an interface '{1}' which has the ByReference attribute: This is not allowed; they are mutually exclusive",
						type,
						@interface));
				}

				_getSingletonInstance.Add(type, method);
			}

			return true;
		}
	}
}