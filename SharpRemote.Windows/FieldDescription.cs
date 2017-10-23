using System.Reflection;
using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="FieldInfo" /> (in that it describes a particular .NET field), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class FieldDescription
	{
		private TypeDescription _fieldType;

		/// <summary>
		///     The id of the <see cref="TypeDescription" /> which describes the type of this field.
		/// </summary>
		[DataMember]
		public int FieldTypeId { get; set; }

		/// <summary>
		///     The type of this field, equivalent of <see cref="FieldInfo.FieldType" />.
		/// </summary>
		public TypeDescription FieldType
		{
			get { return _fieldType; }
			set
			{
				_fieldType = value;
				FieldTypeId = value?.Id ?? -1;
			}
		}

		/// <summary>
		///     The equivalent of <see cref="MemberInfo.Name" />.
		/// </summary>
		[DataMember]
		public string Name { get; set; }
	}
}