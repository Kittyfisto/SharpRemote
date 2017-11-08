using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="FieldInfo" /> (in that it describes a particular .NET field), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class FieldDescription
		: IFieldDescription
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

		/// <inheritdoc />
		[DataMember]
		public string Name { get; set; }

		ITypeDescription IFieldDescription.FieldType => _fieldType;

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0} {1};", _fieldType, Name);
		}

		/// <summary>
		///     Creates a new description for the given field.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
		public static FieldDescription Create(FieldInfo field, IDictionary<string, TypeDescription> typesByAssemblyQualifiedName)
		{
			return new FieldDescription
			{
				Name = field.Name,
				FieldType = TypeDescription.GetOrCreate(field.FieldType, typesByAssemblyQualifiedName)
			};
		}
	}
}