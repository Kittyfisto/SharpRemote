using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using log4net;

namespace SharpRemote.CodeGeneration.Serialization
{
	/// <summary>
	///     Provides access to already compiled serialization methods
	///     and compiles new methods on-demand through a provided <see cref="ISerializationMethodCompiler{T}" />.
	/// </summary>
	internal sealed class SerializationMethodStorage<T>
		: ISerializationMethodStorage<T>
		where T : ISerializationMethods
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly TypeModel _typeModel;
		private readonly ISerializationMethodCompiler<T> _compiler;
		private readonly Dictionary<Type, MethodInfo> _getSingletonInstance;
		private readonly Dictionary<Type, T> _serializationMethods;
		private readonly string _suffix;
		private readonly object _syncRoot;

		/// <summary>
		/// </summary>
		/// <param name="suffix">The suffix used as part of the namespace for serialization methods</param>
		/// <param name="compiler"></param>
		public SerializationMethodStorage(string suffix, ISerializationMethodCompiler<T> compiler)
		{
			if (suffix == null)
				throw new ArgumentNullException(nameof(suffix));
			if (compiler == null)
				throw new ArgumentNullException(nameof(compiler));

			_suffix = suffix;
			_compiler = compiler;
			_syncRoot = new object();
			_typeModel = new TypeModel();
			_serializationMethods = new Dictionary<Type, T>();
			_getSingletonInstance = new Dictionary<Type, MethodInfo>();
		}

		public T GetOrAdd(Type type)
		{
			lock (_syncRoot)
			{
				// Usually we already have generated the methods necessary to serialize / deserialize
				// and thus we can simply retrieve them from the dictionary
				T serializationMethods;
				if (!_serializationMethods.TryGetValue(type, out serializationMethods))
				{
					var typeDescription = _typeModel.Add(type);

					// If that's not the case, then we'll have to generate them.
					// However we need to pay special attention to certain types, for example ByReference
					// types where the serialization method is IDENTICAL for each implementation.
					//
					// Usually we would call PatchType() everytime, however this method is very time-expensive
					// and therefore we will register both the type as well as the patched type, which
					// causes subsequent calls to RegisterType to no longer invoke PatchType.
					//
					// In essence PatchType is only ever invoked ONCE per type instead of for every call to RegisterType.
					var patchedType = PatchType(type);
					if (!_serializationMethods.TryGetValue(patchedType, out serializationMethods))
					{
						var typeName = BuildTypeName(type);
						serializationMethods = _compiler.Prepare(typeName, typeDescription);

						try
						{
							_serializationMethods.Add(type, serializationMethods);
							_compiler.Compile(serializationMethods, this);
						}
						catch (Exception e)
						{
							Log.DebugFormat("Caught unexpected exception while trying to compile serialization methods for '{0}': {1}", typeDescription,
							                e);
							_serializationMethods.Remove(type);
							throw;
						}

						if (type != patchedType)
							_serializationMethods.Add(type, serializationMethods);
					}
				}
				return serializationMethods;
			}
		}

		[Pure]
		private Type PatchType(Type type)
		{
			if (type.Is(typeof(Type)))
				return typeof(Type);

			if (type.GetRealCustomAttribute<ByReferenceAttribute>(inherited: true) != null)
			{
				// Before we accept that this type is ByReference, we should
				// verify that no other constraints are broken (such as also being
				// a singleton type).
				MethodInfo unused;
				if (IsSingleton(type, out unused))
					return type;

				return FindProxyInterface(type);
			}

			return type;
		}

		private string BuildTypeName(Type type)
		{
			var builder = new StringBuilder();
			builder.AppendFormat("SharpRemote.Serialization.{0}", _suffix);
			BuildTypeName(builder, type);
			return builder.ToString();
		}

		private static void BuildTypeName(StringBuilder builder, Type type)
		{
			builder.AppendFormat("{0}.{1}", type.Namespace, type.Name);
			if (type.IsGenericType)
			{
				builder.Append("[");
				var args = type.GetGenericArguments();
				for (var i = 0; i < args.Length; ++i)
				{
					if (i != 0)
						builder.Append(",");

					var innerType = args[i];
					BuildTypeName(builder, innerType);
				}
				builder.Append("]");
			}
		}

		private static Type FindProxyInterface(Type type)
		{
			if (type.GetCustomAttribute<DataContractAttribute>(inherit: true) != null)
				throw new
					ArgumentException(string.Format("The type '{0}' is marked with the [DataContract] as well as [ByReference] attribute, but these are mutually exclusive",
					                                type.FullName));

			Type proxyInterface;
			var attributed = type.GetInterfaces().Where(x => x.GetCustomAttribute<ByReferenceAttribute>() != null).ToList();
			if (attributed.Count > 1)
				throw new ArgumentException(
				                            string.Format(
				                                          "Currently a type may implement only one interface marked with the [ByReference] attribute, but '{0}' implements more than that: {1}",
				                                          type.FullName,
				                                          string.Join(", ", attributed.Select(x => x.FullName))
				                                         )
				                           );

			if (attributed.Count == 0)
			{
				if (type.GetCustomAttribute<ByReferenceAttribute>(inherit: false) == null)
					throw new SystemException(string.Format("Unable to extract the correct proxy interface for type '{0}'",
					                                        type.FullName));

				proxyInterface = type;
			}
			else
			{
				proxyInterface = attributed[index: 0];
			}
			return proxyInterface;
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
					throw new
						ArgumentException(string.Format("The type '{0}' has more than one singleton factory - this is not allowed",
						                                type));

				method = factories[index: 0];
				if (method.ReturnType != type)
					throw new
						ArgumentException(string.Format("The factory method '{0}.{1}' is required to return a value of type '{0}' but doesn't",
						                                type, method));

				var @interface = type.GetInterfaces().FirstOrDefault(x => x.GetCustomAttribute<ByReferenceAttribute>() != null);
				if (@interface != null)
					throw new ArgumentException(string.Format(
					                                          "The type '{0}' both has a method marked with the SingletonFactoryMethod attribute and also implements an interface '{1}' which has the ByReference attribute: This is not allowed; they are mutually exclusive",
					                                          type,
					                                          @interface));

				_getSingletonInstance.Add(type, method);
			}

			return true;
		}

		public bool Contains(Type type)
		{
			lock (_syncRoot)
			{
				return _serializationMethods.ContainsKey(type);
			}
		}
	}
}