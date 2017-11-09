using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote
{
	/// <summary>
	///     TODO: Remove once <see cref="ITypeDescription" /> is used throughout.
	/// </summary>
	internal sealed class TypeInformation
	{
		private readonly Type _elementType;

		public TypeInformation(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			var methods = type.GetMethods().Concat(
			                                       type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
			                                      ).ToList();
			ThrowIfConstraintsAreViolated(methods);

			CollectionType = GetCollectionInterface(type, out _elementType);
			Type = type;
			Fields =
				type.GetFields()
				    .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
				    .ToArray();

			if (IsStack)
				_elementType = Type.GetGenericArguments()[0];
			else if (IsQueue)
				_elementType = Type.GetGenericArguments()[0];

			ThrowIfConstraintsAreViolated(Fields);

			Properties =
				type.GetProperties()
				    .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
				    .ToArray();

			ThrowIfConstraintsAreViolated(Properties);

			if (type.IsArray)
				_elementType = type.GetElementType();
		}

		public Type ElementType => _elementType;

		public Type Type { get; }

		public FieldInfo[] Fields { get; }

		public PropertyInfo[] Properties { get; }

		public string Namespace => Type.Namespace;

		public string Name => Type.Name;

		public bool IsPrimitive => Type.IsPrimitive;

		public bool IsStack => Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Stack<>);

		public bool IsQueue => Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Queue<>);

		public bool IsCollection => CollectionType != null;

		public Type CollectionType { get; }

		public bool IsValueType => Type.IsValueType;

		public bool IsSealed => Type.IsSealed;

		public bool IsArray => Type.IsArray;

		public bool IsGenericType => Type.IsGenericType;

		public Type[] GenericArguments => Type.GetGenericArguments();

		private static Type GetCollectionInterface(Type type, out Type elementType)
		{
			var ifaces = type.GetInterfaces().Where(
			                                        x => x.IsGenericType &&
			                                             x.GetGenericTypeDefinition() == typeof(ICollection<>))
			                 .ToList();

			if (ifaces.Count == 0)
			{
				elementType = null;
				return null;
			}

			if (ifaces.Count > 1)
				throw new
					ArgumentException(string.Format("The type '{0}' implements multiple ICollection<> interfaces, this is not supported by default - you have to register a custom serializer",
					                                type));

			var collectionType = ifaces[index: 0];
			elementType =
				collectionType.GetGenericArguments()
					[0]; //< We have a specific ICollection<> Type, hence there's always exactly one argument
			return collectionType;
		}

		#region Public Methods

		public override string ToString()
		{
			return Type.ToString();
		}

		#endregion

		private static void ThrowIfConstraintsAreViolated(IReadOnlyList<MethodInfo> methods)
		{
			var attributes = new HashSet<Type>();
			var isSingleton = methods.Any(x => x.GetCustomAttribute<SingletonFactoryMethodAttribute>() != null);

			foreach (var method in methods)
			{
				Type attributeType;
				if (IsSerializationCallback(method, out attributeType))
				{
					var type = method.DeclaringType;
					if (isSingleton)
						throw new ArgumentException(
						                            string.Format(
						                                          "The type '{0}.{1}' is a singleton and thus may not contain any serialization callbacks",
						                                          type.Namespace, type.Name));
					var byref = type.GetCustomAttribute<ByReferenceAttribute>();
					if (byref != null)
						throw new ArgumentException(
						                            string.Format(
						                                          "The type '{0}.{1}' is marked with the [ByReference] attribute and thus may not contain methods marked with the [{2}] attribute",
						                                          type.Namespace, type.Name, StripAttribute(attributeType.Name)));
					if (!type.IsClass)
						throw new ArgumentException(
						                            string.Format(
						                                          "The type '{0}.{1}' may not contain methods marked with the [{2}] attribute: Only classes may have these callbacks",
						                                          type.Namespace, type.Name, StripAttribute(attributeType.Name)));

					if (!method.IsPublic)
						throw new ArgumentException(
						                            string.Format(
						                                          "The method '{0}.{1}.{2}()' is marked with the [{3}] attribute and must therefore be publicly accessible",
						                                          type.Namespace, type.Name, method.Name,
						                                          StripAttribute(attributeType.Name)));

					if (method.IsStatic)
						throw new ArgumentException(
						                            string.Format(
						                                          "The method '{0}.{1}.{2}()' is marked with the [{3}] attribute and must therefore be non-static",
						                                          type.Namespace, type.Name, method.Name,
						                                          StripAttribute(attributeType.Name)));

					var parameters = method.GetParameters();
					if (parameters.Length > 0)
						throw new ArgumentException(
						                            string.Format(
						                                          "The method '{0}.{1}.{2}()' is marked with the [{3}] attribute and must therefore be parameterless",
						                                          type.Namespace, type.Name, method.Name,
						                                          StripAttribute(attributeType.Name)));

					if (method.IsGenericMethodDefinition)
						throw new ArgumentException(
						                            string.Format(
						                                          "The method '{0}.{1}.{2}()' is marked with the [{3}] attribute and must therefore be non-generic",
						                                          type.Namespace, type.Name, method.Name,
						                                          StripAttribute(attributeType.Name)));

					if (attributes.Contains(attributeType))
						throw new ArgumentException(
						                            string.Format(
						                                          "The type '{0}.{1}' contains too many methods with the [{2}] attribute: There may not be more than one",
						                                          type.Namespace, type.Name, StripAttribute(attributeType.Name)));

					attributes.Add(attributeType);
				}
			}
		}

		[Pure]
		private static string StripAttribute(string attributeTypeName)
		{
			const string attr = "Attribute";
			if (attributeTypeName.EndsWith(attr))
				return attributeTypeName.Substring(startIndex: 0, length: attributeTypeName.Length - attr.Length);

			return attributeTypeName;
		}

		[Pure]
		private static bool IsSerializationCallback(MethodInfo method, out Type attributeType)
		{
			var beforeSerialize = method.GetCustomAttribute<BeforeSerializeAttribute>();
			var afterSerialize = method.GetCustomAttribute<AfterSerializeAttribute>();
			var beforeDeserialize = method.GetCustomAttribute<BeforeDeserializeAttribute>();
			var afterDeserialize = method.GetCustomAttribute<AfterDeserializeAttribute>();

			if (beforeSerialize != null)
			{
				attributeType = beforeSerialize.GetType();
				return true;
			}

			if (afterSerialize != null)
			{
				attributeType = afterSerialize.GetType();
				return true;
			}

			if (beforeDeserialize != null)
			{
				attributeType = beforeDeserialize.GetType();
				return true;
			}

			if (afterDeserialize != null)
			{
				attributeType = afterDeserialize.GetType();
				return true;
			}

			attributeType = null;
			return false;
		}

		private static void ThrowIfConstraintsAreViolated(IEnumerable<PropertyInfo> properties)
		{
			foreach (var property in properties)
			{
				var type = property.DeclaringType;
				if (!property.CanRead)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] attribute but has no getter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
				if (!property.CanWrite)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] attribute but has no setter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
				if (!property.GetMethod.IsPublic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] has a non-public getter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
				if (!property.SetMethod.IsPublic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] has a non-public setter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
				if (property.GetMethod.IsStatic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] has a static getter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
				if (property.SetMethod.IsStatic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] has a static setter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
			}
		}

		private static void ThrowIfConstraintsAreViolated(IEnumerable<FieldInfo> fields)
		{
			foreach (var field in fields)
			{
				var type = field.DeclaringType;
				if (field.IsStatic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is static - this is not supported",
					                                          type?.Namespace, type?.Name, field.Name));
				if (!field.IsPublic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is not public - this is not supported",
					                                          type?.Namespace, type?.Name, field.Name));
				if (field.IsInitOnly)
					throw new ArgumentException(
					                            string.Format(
					                                          "The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is readonly - this is not supported",
					                                          type?.Namespace, type?.Name, field.Name));
			}
		}

		#region Static Methods

		[Pure]
		public static bool CanBeSerialized(TypeInformation typeInformation)
		{
			return CanBeSerialized(typeInformation.Type);
		}

		[Pure]
		public static bool CanBeSerialized(Type type)
		{
			if (type.IsPrimitive)
				return true;

			if (type.GetCustomAttribute<DataContractAttribute>() != null)
				return true;

			return false;
		}

		#endregion
	}
}