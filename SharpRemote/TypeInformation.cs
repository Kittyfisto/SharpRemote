using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SharpRemote.CodeGeneration;

namespace SharpRemote
{
	internal sealed class TypeInformation
	{
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

		private readonly Type _elementType;
		private readonly FieldInfo[] _fields;
		private readonly PropertyInfo[] _properties;
		private readonly Type _type;
		private readonly Type _collectionType;

		public TypeInformation(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			_collectionType = GetCollectionInterface(type, out _elementType);
			_type = type;
			_fields =
				type.GetFields()
				    .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
				    .ToArray();

			if (IsStack)
			{
				_elementType = _type.GetGenericArguments()[0];
			}

			ThrowIfConstraintsAreViolated(_fields);

			_properties =
				type.GetProperties()
				    .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
				    .ToArray();

			ThrowIfConstraintsAreViolated(_properties);

			if (type.IsArray)
			{
				_elementType = type.GetElementType();
			}
		}

		private static Type GetCollectionInterface(Type type, out Type elementType)
		{
			var ifaces = type.GetInterfaces().Where(
				x => x.IsGenericType &&
					x.GetGenericTypeDefinition() == typeof (ICollection<>))
			    .ToList();

			if (ifaces.Count == 0)
			{
				elementType = null;
				return null;
			}

			if (ifaces.Count > 1)
				throw new ArgumentException(string.Format("The type '{0}' implements multiple ICollection<> interfaces, this is not supported by default - you have to register a custom serializer"));

			var collectionType = ifaces[0];
			elementType = collectionType.GetGenericArguments()[0]; //< We have a specific ICollection<> Type, hence there's always exactly one argument
			return collectionType;
		}

		public Type ElementType
		{
			get { return _elementType; }
		}

		public Type Type
		{
			get { return _type; }
		}

		public FieldInfo[] Fields
		{
			get { return _fields; }
		}

		public PropertyInfo[] Properties
		{
			get { return _properties; }
		}

		public string Namespace
		{
			get { return _type.Namespace; }
		}

		public string Name
		{
			get { return _type.Name; }
		}

		public bool IsPrimitive
		{
			get { return _type.IsPrimitive; }
		}

		public bool IsStack
		{
			get { return _type.IsGenericType && _type.GetGenericTypeDefinition() == typeof (Stack<>); }
		}

		public bool IsCollection
		{
			get { return _collectionType != null; }
		}

		public Type CollectionType
		{
			get { return _collectionType; }
		}

		public bool IsValueType
		{
			get { return _type.IsValueType; }
		}

		public bool IsSealed
		{
			get { return _type.IsSealed; }
		}

		public bool IsArray
		{
			get { return _type.IsArray; }
		}

		public bool IsGenericType
		{
			get { return _type.IsGenericType; }
		}

		public Type[] GenericArguments
		{
			get { return _type.GetGenericArguments(); }
		}

		#region Public Methods

		public override string ToString()
		{
			return _type.ToString();
		}

		#endregion

		private void ThrowIfConstraintsAreViolated(IEnumerable<PropertyInfo> properties)
		{
			foreach (var property in properties)
			{
				Type type = property.DeclaringType;
				if (!property.CanRead)
				{
					throw new ArgumentException(
						string.Format(
							"The property '{0}.{1}.{2}' is marked with the [DataMember] attribute but has no getter - this is not supported",
							type.Namespace, type.Name, property.Name));
				}
				if (!property.CanWrite)
				{
					throw new ArgumentException(
						string.Format(
							"The property '{0}.{1}.{2}' is marked with the [DataMember] attribute but has no setter - this is not supported",
							type.Namespace, type.Name, property.Name));
				}
				if (!property.GetMethod.IsPublic)
				{
					throw new ArgumentException(
						string.Format(
							"The property '{0}.{1}.{2}' is marked with the [DataMember] has a non-public getter - this is not supported",
							type.Namespace, type.Name, property.Name));
				}
				if (!property.SetMethod.IsPublic)
				{
					throw new ArgumentException(
						string.Format(
							"The property '{0}.{1}.{2}' is marked with the [DataMember] has a non-public setter - this is not supported",
							type.Namespace, type.Name, property.Name));
				}
				if (property.GetMethod.IsStatic)
				{
					throw new ArgumentException(
						string.Format(
							"The property '{0}.{1}.{2}' is marked with the [DataMember] has a static getter - this is not supported",
							type.Namespace, type.Name, property.Name));
				}
				if (property.SetMethod.IsStatic)
				{
					throw new ArgumentException(
						string.Format(
							"The property '{0}.{1}.{2}' is marked with the [DataMember] has a static setter - this is not supported",
							type.Namespace, type.Name, property.Name));
				}
			}
		}

		private void ThrowIfConstraintsAreViolated(IEnumerable<FieldInfo> fields)
		{
			foreach (var field in fields)
			{
				Type type = field.DeclaringType;
				if (field.IsStatic)
				{
					throw new ArgumentException(
						string.Format(
							"The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is static - this is not supported",
							type.Namespace, type.Name, field.Name));
				}
				if (!field.IsPublic)
				{
					throw new ArgumentException(
						string.Format(
							"The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is not public - this is not supported",
							type.Namespace, type.Name, field.Name));
				}
				if (field.IsInitOnly)
				{
					throw new ArgumentException(
						string.Format(
							"The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is readonly - this is not supported",
							type.Namespace, type.Name, field.Name));
				}
			}
		}
	}
}