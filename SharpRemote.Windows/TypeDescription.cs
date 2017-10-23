using System;
using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="Type" /> (in that it describes a particular .NET type), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class TypeDescription
	{
		/// <summary>
		///     An id which differentiates this object amongst all others for the same
		///     <see cref="TypeModel" />.
		/// </summary>
		[DataMember]
		public int Id { get; set; }

		/// <summary>
		///     Equivalent of <see cref="Type.AssemblyQualifiedName" />.
		/// </summary>
		[DataMember]
		public string AssemblyQualifiedName { get; set; }

		/// <summary>
		///     Equivalent of <see cref="Type.IsClass" />.
		/// </summary>
		[DataMember]
		public bool IsClass { get; set; }

		/// <summary>
		///     Equivalent of <see cref="Type.IsEnum" />.
		/// </summary>
		[DataMember]
		public bool IsEnum { get; set; }

		/// <summary>
		///     Equivalent of <see cref="Type.IsInterface" />.
		/// </summary>
		[DataMember]
		public bool IsInterface { get; set; }

		/// <summary>
		///     Equivalent of <see cref="Type.IsValueType" />.
		/// </summary>
		[DataMember]
		public bool IsValueType { get; set; }

		/// <summary>
		///     The list of public non-static properties with the <see cref="DataMemberAttribute" />.
		/// </summary>
		[DataMember]
		public PropertyDescription[] Properties { get; set; }

		/// <summary>
		///     The list of public non-static fields with the <see cref="DataMemberAttribute" />.
		/// </summary>
		[DataMember]
		public FieldDescription[] Fields { get; set; }
	}
}