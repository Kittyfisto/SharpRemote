using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="Type" /> (in that it describes a particular .NET type), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	public interface ITypeDescription
	{
		/// <summary>
		///     Equivalent of <see cref="Type.AssemblyQualifiedName" />.
		/// </summary>
		string AssemblyQualifiedName { get; }

		/// <summary>
		///     A classification of this type which is helpful for the serializer.
		/// </summary>
		SerializationType SerializationType { get; }

		/// <summary>
		///     Equivalent of <see cref="Type.IsClass" />.
		/// </summary>
		bool IsClass { get; }

		/// <summary>
		///     Equivalent of <see cref="Type.IsEnum" />.
		/// </summary>
		bool IsEnum { get; }

		/// <summary>
		///     Equivalent of <see cref="Type.IsInterface" />.
		/// </summary>
		bool IsInterface { get; }

		/// <summary>
		///     Equivalent of <see cref="Type.IsValueType" />.
		/// </summary>
		bool IsValueType { get; }

		/// <summary>
		///     The list of public non-static properties with the <see cref="DataMemberAttribute" />.
		/// </summary>
		IReadOnlyList<PropertyDescription> Properties { get; }

		/// <summary>
		///     The list of public non-static fields with the <see cref="DataMemberAttribute" />.
		/// </summary>
		IReadOnlyList<FieldDescription> Fields { get; }

		/// <summary>
		/// The list of public non-static methods in case this is a <see cref="SharpRemote.SerializationType.ByReference"/> type.
		/// </summary>
		IReadOnlyList<MethodDescription> Methods { get; }
	}
}