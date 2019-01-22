using System;
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
		private readonly FieldInfo _field;
		private TypeDescription _fieldType;
		private int? _fieldTypeId;

		/// <summary>
		/// 
		/// </summary>
		public FieldDescription()
		{ }

		private FieldDescription(FieldInfo field)
		{
			_field = field;
			Name = field.Name;

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

		/// <summary>
		///     The id of the <see cref="SharpRemote.TypeDescription" /> which describes the type of this field.
		/// </summary>
		[DataMember]
		public int FieldTypeId
		{
			get { return _fieldTypeId ?? _fieldType?.Id ?? 0; }
			set { _fieldTypeId = value; }
		}

		/// <summary>
		///     The type of this field, equivalent of <see cref="FieldInfo.FieldType" />.
		/// </summary>
		public TypeDescription FieldType
		{
			get { return _fieldType; }
			set
			{
				_fieldType = value;
				if (value != null)
				{
					var id = value.Id;
					if (id > 0)
						_fieldTypeId = id;
				}
			}
		}

		/// <inheritdoc />
		[DataMember]
		public string Name { get; set; }

		/// <inheritdoc />
		public ITypeDescription TypeDescription => _fieldType;

		/// <summary>
		/// 
		/// </summary>
		public Type Type => _field.FieldType;

		/// <inheritdoc />
		public MemberInfo MemberInfo => _field;

		ITypeDescription IFieldDescription.FieldType => _fieldType;

		/// <summary>
		/// 
		/// </summary>
		public FieldInfo Field => _field;

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
			return new FieldDescription(field)
			{
				FieldType = SharpRemote.TypeDescription.GetOrCreate(field.FieldType, typesByAssemblyQualifiedName)
			};
		}
	}
}