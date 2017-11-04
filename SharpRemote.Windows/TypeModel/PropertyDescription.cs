using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

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
		private TypeDescription _propertyType;

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

		/// <inheritdoc />
		[DataMember]
		public string Name { get; set; }

		ITypeDescription IPropertyDescription.PropertyType => _propertyType;

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0} {1} {{ get; set; }}", PropertyType, Name);
		}

		/// <summary>
		///     Creates a new description for the given property.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
		public static PropertyDescription Create(PropertyInfo property, IDictionary<string, TypeDescription> typesByAssemblyQualifiedName)
		{
			var propertyType = property.PropertyType;
			var propertyTypeName = propertyType.AssemblyQualifiedName;
			TypeDescription type = null;
			if (propertyTypeName != null)
				if (!typesByAssemblyQualifiedName.TryGetValue(propertyTypeName, out type))
				{
					// The ctor add itself the given dictionary, so we don't have to add it ourselves
					// (It needs to be this way because a type is allowed to reference itself).
					type = TypeDescription.Create(propertyType, typesByAssemblyQualifiedName);
				}

			return new PropertyDescription
			{
				Name = property.Name,
				PropertyType = type
			};
		}
	}
}