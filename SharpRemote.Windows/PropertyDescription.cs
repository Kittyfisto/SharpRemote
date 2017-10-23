using System.Reflection;
using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="PropertyInfo" /> (in that it describes a particular .NET property), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class PropertyDescription
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

		/// <summary>
		///     The equivalent of <see cref="MemberInfo.Name" />.
		/// </summary>
		[DataMember]
		public string Name { get; set; }
	}
}