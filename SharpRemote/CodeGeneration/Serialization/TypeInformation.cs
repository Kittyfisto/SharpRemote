using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;

namespace SharpRemote.CodeGeneration.Serialization
{
	public sealed class TypeInformation
	{
		private readonly Type _type;
		private readonly ConstructorInfo _ctor;
		private readonly FieldInfo[] _fields;
		private readonly PropertyInfo[] _properties;

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

		public static bool RequiresConstructor(Type type)
		{
			if (type.IsPrimitive)
				return false;

			if (type == typeof (string))
				return false;

			if (type == typeof (IPAddress))
				return false;

			if (type.IsValueType)
				return false;

			if (type.IsAbstract)
				return false;

			return true;
		}

		[Pure]
		public static bool IsNativelySupportedType(Type type)
		{
			if (type.IsPrimitive)
				return true;

			if (type == typeof (string))
				return true;

			if (type == typeof (IPAddress))
				return true;

			return false;
		}

		public TypeInformation(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (!IsNativelySupportedType(type) && type.GetCustomAttribute<DataContractAttribute>() == null)
				throw new ArgumentException(string.Format("The type '{0}.{1}' is missing the [DataContract] attribute - this is not supported", type.Namespace, type.Name));

			_type = type;
			_fields =
				type.GetFields()
					.Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
					.ToArray();

			ThrowIfConstraintsAreViolated(_fields);

			_properties =
				type.GetProperties()
					.Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
					.ToArray();

			ThrowIfConstraintsAreViolated(_properties);

			if (RequiresConstructor(type))
			{
				_ctor = type.GetConstructor(new Type[0]);
				if (_ctor == null)
					throw new ArgumentException(string.Format("Type '{0}' is missing a parameterless constructor", type));
			}
		}

		private void ThrowIfConstraintsAreViolated(IEnumerable<PropertyInfo> properties)
		{
			foreach (var property in properties)
			{
				var type = property.DeclaringType;
				if (!property.CanRead)
				{
					throw new ArgumentException(string.Format("The property '{0}.{1}.{2}' is marked with the [DataMember] attribute but has no getter - this is not supported", type.Namespace, type.Name, property.Name));
				}
				if (!property.CanWrite)
				{
					throw new ArgumentException(string.Format("The property '{0}.{1}.{2}' is marked with the [DataMember] attribute but has no setter - this is not supported", type.Namespace, type.Name, property.Name));
				}
				if (!property.GetMethod.IsPublic)
				{
					throw new ArgumentException(string.Format("The property '{0}.{1}.{2}' is marked with the [DataMember] has a non-public getter - this is not supported", type.Namespace, type.Name, property.Name));
				}
				if (!property.SetMethod.IsPublic)
				{
					throw new ArgumentException(string.Format("The property '{0}.{1}.{2}' is marked with the [DataMember] has a non-public setter - this is not supported", type.Namespace, type.Name, property.Name));
				}
				if (property.GetMethod.IsStatic)
				{
					throw new ArgumentException(string.Format("The property '{0}.{1}.{2}' is marked with the [DataMember] has a static getter - this is not supported", type.Namespace, type.Name, property.Name));
				}
				if (property.SetMethod.IsStatic)
				{
					throw new ArgumentException(string.Format("The property '{0}.{1}.{2}' is marked with the [DataMember] has a static setter - this is not supported", type.Namespace, type.Name, property.Name));
				}
			}
		}

		private void ThrowIfConstraintsAreViolated(IEnumerable<FieldInfo> fields)
		{
			foreach (var field in fields)
			{
				var type = field.DeclaringType;
				if (field.IsStatic)
				{
					throw new ArgumentException(string.Format("The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is static - this is not supported", type.Namespace, type.Name, field.Name));
				}
				if (!field.IsPublic)
				{
					throw new ArgumentException(string.Format("The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is not public - this is not supported", type.Namespace, type.Name, field.Name));
				}
				if (field.IsInitOnly)
				{
					throw new ArgumentException(string.Format("The field '{0}.{1}.{2}' is marked with the [DataMember] attribute but is readonly - this is not supported", type.Namespace, type.Name, field.Name));
				}
			}
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

		public bool IsValueType
		{
			get { return _type.IsValueType; }
		}

		public bool IsSealed
		{
			get { return _type.IsSealed; }
		}

		public ConstructorInfo Constructor
		{
			get { return _ctor; }
		}

		public override string ToString()
		{
			return _type.ToString();
		}
	}
}