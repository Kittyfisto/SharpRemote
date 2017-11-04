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
		public IMethodDescription GetMethod { get; set; }

		/// <inheritdoc />
		[DataMember]
		public IMethodDescription SetMethod { get; set; }

		/// <inheritdoc />
		[DataMember]
		public string Name { get; set; }

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
		/// <param name="property"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
		public static PropertyDescription Create(PropertyInfo property, IDictionary<string, TypeDescription> typesByAssemblyQualifiedName)
		{
			return new PropertyDescription
			{
				Name = property.Name,
				PropertyType = TypeDescription.GetOrCreate(property.PropertyType, typesByAssemblyQualifiedName),
				GetMethod = property.GetMethod != null ? MethodDescription.Create(property.GetMethod, typesByAssemblyQualifiedName) : null,
				SetMethod = property.SetMethod != null ? MethodDescription.Create(property.SetMethod, typesByAssemblyQualifiedName) : null
			};
		}
	}
}