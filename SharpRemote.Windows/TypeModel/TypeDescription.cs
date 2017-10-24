using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
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
		///     A classification of this type which is helpful for the serializer.
		/// </summary>
		[DataMember]
		public SerializationType SerializationType { get; set; }

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

		/// <summary>
		/// The list of public non-static methods in case this is a <see cref="SerializationType.ByReference"/> type.
		/// </summary>
		[DataMember]
		public MethodDescription[] Methods { get; set; }

		/// <summary>
		///     Creates a new description for the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
		[Pure]
		public static TypeDescription Create(Type type, IDictionary<string, TypeDescription> typesByAssemblyQualifiedName)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			var assemblyQualifiedName = type.AssemblyQualifiedName;
			if (assemblyQualifiedName == null)
				throw new ArgumentException("Type.AssemblyQualifiedName should not be null");

			var description = new TypeDescription
			{
				AssemblyQualifiedName = assemblyQualifiedName
			};

			typesByAssemblyQualifiedName.Add(assemblyQualifiedName, description);

			description.Fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Select(x => FieldDescription.Create(x, typesByAssemblyQualifiedName)).ToArray();
			description.Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Select(x => PropertyDescription.Create(x, typesByAssemblyQualifiedName)).ToArray();
			return description;
		}
	}
}