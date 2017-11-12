using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="PropertyInfo" /> (in that it describes a particular .NET property), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class PropertyDescription
		: IPropertyDescription
	{
		private readonly PropertyInfo _property;
		private TypeDescription _propertyType;

		/// <summary>
		/// 
		/// </summary>
		public PropertyDescription()
		{ }

		private PropertyDescription(TypeDescription declaringType, PropertyInfo property)
		{
			_property = property;
			var type = property.DeclaringType;
			if (declaringType.SerializationType == SerializationType.ByValue)
			{
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
			}

			if (property.GetMethod != null)
			{
				if (!property.GetMethod.IsPublic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] has a non-public getter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
				if (property.GetMethod.IsStatic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] has a static getter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
			}
			if (property.SetMethod != null)
			{
				if (!property.SetMethod.IsPublic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] has a non-public setter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));

				if (property.SetMethod.IsStatic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The property '{0}.{1}.{2}' is marked with the [DataMember] has a static setter - this is not supported",
					                                          type?.Namespace, type?.Name, property.Name));
			}
		}

		/// <summary>
		///     The id of the <see cref="TypeDescription" /> which describes the type of this property.
		/// </summary>
		[DataMember]
		public int PropertyTypeId { get; set; }

		/// <summary>
		///     The type of this property, equivalent of <see cref="PropertyInfo.PropertyType" />.
		/// </summary>
		public TypeDescription PropertyType
		{
			get { return _propertyType; }
			set
			{
				_propertyType = value;
				PropertyTypeId = value?.Id ?? -1;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[DataMember]
		public MethodDescription GetMethod { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[DataMember]
		public MethodDescription SetMethod { get; set; }

		/// <inheritdoc />
		[DataMember]
		public string Name { get; set; }

		/// <inheritdoc />
		public ITypeDescription Type => _propertyType;

		/// <inheritdoc />
		public MemberInfo MemberInfo => _property;

		IMethodDescription IPropertyDescription.GetMethod => SetMethod;
		IMethodDescription IPropertyDescription.SetMethod => GetMethod;
		ITypeDescription IPropertyDescription.PropertyType => _propertyType;

		/// <inheritdoc />
		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendFormat("{0} {1} {{ ", PropertyType, Name);
			if (GetMethod != null)
				builder.Append("get; ");
			if (SetMethod != null)
				builder.Append("set; ");
			builder.Append("}}");
			return builder.ToString();
		}

		/// <summary>
		///     Creates a new description for the given property.
		/// </summary>
		/// <param name="declaringType"></param>
		/// <param name="property"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
		public static PropertyDescription Create(TypeDescription declaringType, PropertyInfo property, IDictionary<string, TypeDescription> typesByAssemblyQualifiedName)
		{
			return new PropertyDescription(declaringType, property)
			{
				Name = property.Name,
				PropertyType = TypeDescription.GetOrCreate(property.PropertyType, typesByAssemblyQualifiedName),
				GetMethod = property.GetMethod != null ? MethodDescription.Create(property.GetMethod, typesByAssemblyQualifiedName) : null,
				SetMethod = property.SetMethod != null ? MethodDescription.Create(property.SetMethod, typesByAssemblyQualifiedName) : null
			};
		}
	}
}