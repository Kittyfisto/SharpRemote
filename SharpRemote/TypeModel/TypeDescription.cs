using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using log4net.Core;
using SharpRemote.Attributes;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="Type" /> (in that it describes a particular .NET type), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class TypeDescription
		: ITypeDescription
	{
		/// <summary>
		///     The list of types for which SharpRemote has built-in (de)serialization methods.
		/// </summary>
		private static readonly HashSet<Type> BuiltInTypes;

		private readonly Type _type;
		private readonly Type _byReferenceInterfaceType;
		private TypeDescription _baseType;

		static TypeDescription()
		{
			BuiltInTypes = new HashSet<Type>
			{
				typeof(void),
				typeof(string),
				typeof(decimal),
				typeof(DateTime),
				typeof(Level)
			};
		}

		/// <summary>
		///     Initializes this object.
		/// </summary>
		public TypeDescription()
		{ }

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="byReferenceInterfaceType"></param>
		private TypeDescription(Type type, Type byReferenceInterfaceType)
		{
			_type = type;
			_byReferenceInterfaceType = byReferenceInterfaceType;
		}

		/// <summary>
		/// The type being described by this object.
		/// </summary>
		public Type Type => _type;

		/// <summary>
		/// The type being described by this object.
		/// </summary>
		public Type ByReferenceInterfaceType => _byReferenceInterfaceType;

		/// <summary>
		///     An id which differentiates this object amongst all others for the same
		///     <see cref="TypeModel" />.
		/// </summary>
		[DataMember]
		public int Id { get; set; }

		/// <summary>
		///     The underlying type used for storing values.
		/// </summary>
		/// <remarks>
		///     Relevant for enums.
		/// </remarks>
		[DataMember]
		public TypeDescription StorageType { get; set; }

		/// <summary>
		///     The list of public non-static properties with the <see cref="DataMemberAttribute" />.
		/// </summary>
		[DataMember]
		public PropertyDescription[] Properties { get; set; }

		/// <summary>
		///     The list of public non-static fields with the <see cref="DataMemberAttribute" />.
		/// </summary>
		[DataMember]
		public FieldDescription[] Fields { get; set; }

		/// <summary>
		///     The list of public non-static methods in case this is a <see cref="SharpRemote.SerializationType.ByReference" />
		///     type.
		/// </summary>
		[DataMember]
		public MethodDescription[] Methods { get; set; }

		/// <summary>
		///    The list of enum values.
		/// </summary>
		[DataMember]
		public EnumValueDescription[] EnumValues { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[DataMember]
		public int BaseTypeId { get; set; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.BaseType" />.
		/// </summary>
		public TypeDescription BaseType
		{
			get { return _baseType; }
			set
			{
				_baseType = value;
				BaseTypeId = value?.Id ?? 0;
			}
		}

		ITypeDescription ITypeDescription.BaseType => _baseType;

		/// <inheritdoc />
		[DataMember]
		public string AssemblyQualifiedName { get; set; }

		/// <inheritdoc />
		[DataMember]
		public SerializationType SerializationType { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsClass { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsEnum { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsEnumerable { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsInterface { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsValueType { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsSealed { get; set; }

		/// <inheritdoc />
		public bool IsBuiltIn { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsGenericType { get; set; }

		ITypeDescription ITypeDescription.StorageType => StorageType;

		IReadOnlyList<IPropertyDescription> ITypeDescription.Properties => Properties;

		IReadOnlyList<IFieldDescription> ITypeDescription.Fields => Fields;

		IReadOnlyList<IMethodDescription> ITypeDescription.Methods => Methods;

		IReadOnlyList<IEnumValueDescription> ITypeDescription.EnumValues => EnumValues;

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0}", AssemblyQualifiedName);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
		public static TypeDescription GetOrCreate(Type type,
		                                          IDictionary<string, TypeDescription> typesByAssemblyQualifiedName)
		{
			TypeDescription description;
			if (!typesByAssemblyQualifiedName.TryGetValue(type.AssemblyQualifiedName, out description))
			{
				return Create(type, typesByAssemblyQualifiedName);
			}
			return description;
		}

		/// <summary>
		///     Creates a new description for the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <param name="assumeProxy"></param>
		/// <returns></returns>
		public static TypeDescription Create(Type type,
		                                     IDictionary<string, TypeDescription> typesByAssemblyQualifiedName,
		                                     bool assumeProxy = false)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			var assemblyQualifiedName = type.AssemblyQualifiedName;
			if (assemblyQualifiedName == null)
				throw new ArgumentException("Type.AssemblyQualifiedName should not be null");

			bool builtIn, isEnumerable;
			MethodInfo singletonAccessor;
			Type byReferenceInterface;
			var serializerType = GetSerializationType(type,
													  assumeProxy,
			                                          out builtIn,
			                                          out isEnumerable,
			                                          out singletonAccessor,
			                                          out byReferenceInterface);

			var description = new TypeDescription(type, byReferenceInterface)
			{
				AssemblyQualifiedName = assemblyQualifiedName,
				SerializationType = serializerType
			};

			typesByAssemblyQualifiedName.Add(assemblyQualifiedName, description);

			var serializationCallbacks = GetSerializationCallbacks(type, typesByAssemblyQualifiedName);
			switch (description.SerializationType)
			{
				case SerializationType.ByValue:
					// TODO: Throw when ByValue rules are violated
					description.Fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
					                         .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
					                         .Select(x => FieldDescription.Create(x, typesByAssemblyQualifiedName)).ToArray();
					description.Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
					                             .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
					                             .Select(x => PropertyDescription.Create(description, x, typesByAssemblyQualifiedName)).ToArray();
					description.Methods = serializationCallbacks;
					break;

				case SerializationType.ByReference:
					if (serializationCallbacks.Any())
						throw new ArgumentException(
						                            string.Format(
						                                          "The type '{0}.{1}' is marked with the [ByReference] attribute and thus may not contain serialization callback methods",
						                                          type.Namespace, type.Name));
					
					description.Fields = new FieldDescription[0];
					description.Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					                             .Select(x => PropertyDescription.Create(description, x, typesByAssemblyQualifiedName)).ToArray();
					description.Methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					                          .Where(x => !x.IsSpecialName)
					                          .Select(x => MethodDescription.Create(x, typesByAssemblyQualifiedName)).ToArray();
					break;

				case SerializationType.Singleton:
					if (serializationCallbacks.Any())
						throw new ArgumentException(
						                            string.Format(
						                                          "The type '{0}.{1}' is a singleton and thus may not contain any serialization callbacks",
						                                          type.Namespace, type.Name));

					description.Fields = new FieldDescription[0];
					description.Properties = new PropertyDescription[0];
					description.Methods = new[] {MethodDescription.Create(singletonAccessor, typesByAssemblyQualifiedName)};
					break;

				case SerializationType.Unknown:
					description.Fields = new FieldDescription[0];
					description.Properties = new PropertyDescription[0];
					description.Methods = new MethodDescription[0];
					break;

				case SerializationType.NotSerializable:
					// TODO: Throw proper exception with proper error message
					throw new NotImplementedException();

				default:
					throw new NotImplementedException();
			}

			if (IsInterestingBaseType(type.BaseType))
				description.BaseType = GetOrCreate(type.BaseType, typesByAssemblyQualifiedName);
			description.IsBuiltIn = builtIn;
			description.IsValueType = type.IsValueType;
			description.IsClass = type.IsClass;
			description.IsInterface = type.IsInterface;
			description.IsEnum = type.IsEnum;
			description.IsEnumerable = isEnumerable;
			description.IsSealed = type.IsSealed;
			description.IsGenericType = type.IsGenericType;

			if (type.IsEnum)
			{
				var storageType = Enum.GetUnderlyingType(type);
				description.StorageType = GetOrCreate(storageType, typesByAssemblyQualifiedName);

				var values = Enum.GetValues(type).OfType<object>().ToArray();
				var names = Enum.GetNames(type);
				var descriptions = new EnumValueDescription[values.Length];
				for (int i = 0; i < names.Length; ++i)
				{
					descriptions[i] = EnumValueDescription.Create(storageType, values[i], names[i]);
				}

				description.EnumValues = descriptions;
			}

			return description;
		}

		private static MethodDescription[] GetSerializationCallbacks(Type type,
		                                                             IDictionary<string, TypeDescription>
			                                                             typesByAssemblyQualifiedName)
		{
			var attributes = new HashSet<SpecialMethod>();
			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			var descriptions = new List<MethodDescription>();
			foreach (var method in methods)
			{
				var attribute = method.GetCustomAttribute<SerializationMethodAttribute>();
				if (attribute != null)
				{
					var methodType = attribute.Method;
					if (attributes.Contains(methodType))
						throw new ArgumentException(
						                            string.Format(
						                                          "The type '{0}.{1}' contains too many methods with the [{2}] attribute: There may not be more than one",
						                                          type.Namespace, type.Name, methodType));
					attributes.Add(methodType);
					var description = MethodDescription.Create(method, typesByAssemblyQualifiedName);
					descriptions.Add(description);
				}
			}
			return descriptions.ToArray();
		}

		[Pure]
		private static bool IsInterestingBaseType(Type baseType)
		{
			if (baseType == null)
				return false;
			if (baseType == typeof(object))
				return false;
			if (baseType == typeof(ValueType))
				return false;
			if (baseType == typeof(Enum))
				return false;
			return true;
		}

		[Pure]
		private static SerializationType GetSerializationType(Type type,
		                                                      bool assumeProxy,
		                                                      out bool builtIn,
		                                                      out bool isEnumerable,
		                                                      out MethodInfo singletonAccessor,
		                                                      out Type byReferenceInterface)
		{
			if (type.IsPrimitive ||
			    BuiltInTypes.Contains(type) ||
			    IsException(type) ||
			    type.IsEnum)
			{
				builtIn = true;
				isEnumerable = false;
				singletonAccessor = null;
				byReferenceInterface = null;
				return SerializationType.ByValue;
			}
			builtIn = false;

			var dataContract = type.GetCustomAttribute<DataContractAttribute>();
			var byReference = type.GetCustomAttribute<ByReferenceAttribute>();
			if (dataContract != null && byReference != null)
				throw new NotImplementedException();

			if (dataContract != null)
			{
				isEnumerable = false;
				singletonAccessor = null;
				byReferenceInterface = null;
				return SerializationType.ByValue;
			}

			if (byReference != null)
			{
				isEnumerable = false;
				singletonAccessor = null;
				byReferenceInterface = type.GetInterfaces().FirstOrDefault(x => x.GetCustomAttribute<ByReferenceAttribute>() != null);
				return SerializationType.ByReference;
			}

			// We test for implementations of IEnumerable *after*
			// we've ruled out DataContract classes so we don't accidentally
			// ignore [DataContract] on a type which happens to implement
			// IEnumerable.
			if (IsEnumeration(type))
			{
				builtIn = true;
				isEnumerable = true;
				singletonAccessor = null;
				byReferenceInterface = null;
				return SerializationType.ByValue;
			}
			isEnumerable = false;

			if (assumeProxy && type.IsInterface)
			{
				builtIn = false;
				isEnumerable = false;
				singletonAccessor = null;
				byReferenceInterface = type;
				return SerializationType.ByReference;
			}

			if ((type.IsClass || type.IsInterface) &&
			    !type.IsSealed)
			{
				// The type's serialization is not known and due to being non-sealed
				// the concrete type (or any of it's base classes / interface) will determine
				// the serialization.
				builtIn = false;
				isEnumerable = false;
				singletonAccessor = null;
				byReferenceInterface = null;
				return SerializationType.Unknown;
			}

			var factories = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
			                    .Where(x => x.GetCustomAttribute<SingletonFactoryMethodAttribute>() != null)
			                    .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
			                                .Where(x => x.GetCustomAttribute<SingletonFactoryMethodAttribute>() != null)
			                                .Select(x => x.GetMethod)).ToList();

			if (factories.Count == 0)
				throw new
					ArgumentException(string.Format("The type '{0}.{1}' is missing the [DataContract] or [ByReference] attribute, nor is there a custom-serializer available for this type",
					                                type.Namespace,
					                                type.Name));

			if (factories.Count > 1)
				throw new ArgumentException(string.Format("The type '{0}' has more than one singleton factory - this is not allowed", type));

			singletonAccessor = factories[0];
			if (singletonAccessor.ReturnType != type)
				throw new ArgumentException(string.Format("The factory method '{0}.{1}' is required to return a value of type '{0}' but doesn't", type, singletonAccessor));

			var @interface = type.GetInterfaces().FirstOrDefault(x => x.GetCustomAttribute<ByReferenceAttribute>() != null);
			if (@interface != null)
			{
				throw new ArgumentException(string.Format(
				                                          "The type '{0}' both has a method marked with the SingletonFactoryMethod attribute and also implements an interface '{1}' which has the ByReference attribute: This is not allowed; they are mutually exclusive",
				                                          type,
				                                          @interface));
			}

			byReferenceInterface = null;
			return SerializationType.Singleton;
		}

		/// <summary>
		/// Tests if the given type is <see cref="Exception"/> or inherits from it.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsException(Type type)
		{
			// A type is an exception if is System.Exception or any of its base types are.
			while (type != null && type != typeof(object))
			{
				if (type == typeof(Exception))
					return true;

				type = type.BaseType;
			}

			return false;
		}

		[Pure]
		private static bool IsEnumeration(Type type)
		{
			if (type == typeof(IEnumerable))
				return true;

			var interfaces = type.GetInterfaces();
			foreach (var @interface in interfaces)
			{
				if (@interface == typeof(IEnumerable))
					return true;
			}

			return false;
		}
	}
}