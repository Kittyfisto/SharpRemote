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
		: ITypeDescription
	{
		private static readonly HashSet<Type> BuiltInTypes;

		static TypeDescription()
		{
			BuiltInTypes = new HashSet<Type>
			{
				typeof(void),
				typeof(string)
			};
		}

		/// <summary>
		///     An id which differentiates this object amongst all others for the same
		///     <see cref="TypeModel" />.
		/// </summary>
		[DataMember]
		public int Id { get; set; }

		/// <inheritdoc />
		[DataMember]
		public string AssemblyQualifiedName { get; set; }

		/// <inheritdoc />
		[DataMember]
		public SerializationType SerializationType { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsClass { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsEnum { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsInterface { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsValueType { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsSealed { get; set; }

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
		/// The list of public non-static methods in case this is a <see cref="SharpRemote.SerializationType.ByReference"/> type.
		/// </summary>
		[DataMember]
		public MethodDescription[] Methods { get; set; }

		IReadOnlyList<IPropertyDescription> ITypeDescription.Properties => Properties;

		IReadOnlyList<IFieldDescription> ITypeDescription.Fields => Fields;

		IReadOnlyList<IMethodDescription> ITypeDescription.Methods => Methods;

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0}", AssemblyQualifiedName);
		}

		/// <summary>
		///     Creates a new description for the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
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

			description.SerializationType = GetSerializationType(type);
			description.IsValueType = type.IsValueType;
			description.IsClass = type.IsClass;
			description.IsInterface = type.IsInterface;
			description.IsEnum = type.IsEnum;
			description.IsSealed = type.IsSealed;
			description.Fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
			                         .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
			                         .Select(x => FieldDescription.Create(x, typesByAssemblyQualifiedName)).ToArray();
			description.Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			                             .Where(x => x.GetCustomAttribute<DataMemberAttribute>() != null)
			                             .Select(x => PropertyDescription.Create(x, typesByAssemblyQualifiedName)).ToArray();
			description.Methods = new MethodDescription[0];
			return description;
		}

		[Pure]
		private static SerializationType GetSerializationType(Type type)
		{
			if (IsBuiltIn(type))
			{
				return SerializationType.BuiltIn;
			}

			var attr = type.GetCustomAttribute<DataContractAttribute>();
			if (attr != null)
			{
				return SerializationType.DataContract;
			}

			throw new NotImplementedException();
		}

		private static bool IsBuiltIn(Type type)
		{
			if (type.IsPrimitive)
				return true;

			if (BuiltInTypes.Contains(type))
				return true;

			return false;
		}
	}
}