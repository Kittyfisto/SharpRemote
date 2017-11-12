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
		/// The type being described by this object.
		/// </summary>
		Type Type { get; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.BaseType" />.
		/// </summary>
		ITypeDescription BaseType { get; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.AssemblyQualifiedName" />.
		/// </summary>
		string AssemblyQualifiedName { get; }

		/// <summary>
		///     A classification of this type which is helpful for the serializer.
		/// </summary>
		SerializationType SerializationType { get; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.IsClass" />.
		/// </summary>
		bool IsClass { get; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.IsEnum" />.
		/// </summary>
		bool IsEnum { get; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.IsInterface" />.
		/// </summary>
		bool IsInterface { get; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.IsValueType" />.
		/// </summary>
		bool IsValueType { get; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.IsSealed" />.
		/// </summary>
		bool IsSealed { get; }

		/// <summary>
		///     The type doesn't strictly adhere to the rules of its <see cref="SerializationType" />,
		///     however it's part of the .NET framework and thus serialization methods have been built-in.
		/// </summary>
		bool IsBuiltIn { get; }

		/// <summary>
		///     Equivalent of <see cref="System.Type.IsGenericType" />.
		/// </summary>
		bool IsGenericType { get; }

		/// <summary>
		///     The list of public non-static properties with the <see cref="DataMemberAttribute" />.
		/// </summary>
		IReadOnlyList<IPropertyDescription> Properties { get; }

		/// <summary>
		///     The list of public non-static fields with the <see cref="DataMemberAttribute" />.
		/// </summary>
		IReadOnlyList<IFieldDescription> Fields { get; }

		/// <summary>
		///     The list of public non-static methods in case this is a <see cref="SharpRemote.SerializationType.ByReference" />
		///     type.
		/// </summary>
		IReadOnlyList<IMethodDescription> Methods { get; }
	}
}