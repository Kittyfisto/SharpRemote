using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.Serialization;
using log4net;
using SharpRemote.Attributes;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     A representation of all types registered with a <see cref="ISerializer" />.
	///     This representation describes each type (as far as serialization is concerned) and
	///     may be serialized/deserialized *without* requiring that the types describes by this type model
	///     can be loaded.
	/// </summary>
	[DataContract]
	public sealed class TypeModel
		: ITypeModel
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Dictionary<string, TypeDescription> _typesByAssemblyQualifiedName;
		private int _nextId;
		private List<TypeDescription> _types;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		public TypeModel()
		{
			_typesByAssemblyQualifiedName = new Dictionary<string, TypeDescription>();
			_types = new List<TypeDescription>();
			_nextId = 1;
		}

		/// <inheritdoc />
		[DataMember]
		public IReadOnlyList<TypeDescription> Types
		{
			get { return _types; }
			set { _types = new List<TypeDescription>(value); }
		}

		/// <summary>
		///     Adds the given <typeparamref name="T" /> to this model.
		/// </summary>
		/// <param name="assumeProxy"></param>
		/// <typeparam name="T"></typeparam>
		public ITypeDescription Add<T>(bool assumeProxy = false)
		{
			return Add(typeof(T), assumeProxy);
		}

		/// <summary>
		///     Adds the given <paramref name="type" /> to this model.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="assumeProxy"></param>
		/// <exception cref="ArgumentNullException">When <paramref name="type" /> is null</exception>
		public ITypeDescription Add(Type type, bool assumeProxy = false)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			var name = type.AssemblyQualifiedName;
			if (name == null)
				throw new ArgumentException();

			TypeDescription typeDescription;
			if (!_typesByAssemblyQualifiedName.TryGetValue(name, out typeDescription))
			{
				var typesByAssemblyQualifiedName = CreateTypeCacheClone();

				// We work on a local copy so that:
				// A) In case any constraint is violated, the type model is NEVER modified
				// B) We know exactly which types to add without having to do an O(N) search of _types
				typeDescription = TypeDescription.Create(type, typesByAssemblyQualifiedName, assumeProxy);
				typeDescription.Id = GetNextId();

				// It's likely that we had to expand the type model beyond the given type, all of those additional types
				// have now been added to the local dictionary, but not this type model (yet):
				foreach (var description in typesByAssemblyQualifiedName.Values)
				{
					var assemblyQualifiedName = description.AssemblyQualifiedName;
					if (!_typesByAssemblyQualifiedName.ContainsKey(assemblyQualifiedName))
					{
						description.Id = GetNextId();

						_typesByAssemblyQualifiedName.Add(assemblyQualifiedName, description);
						_types.Add(description);
					}
				}
			}

			return typeDescription;
		}

		private Dictionary<string, TypeDescription> CreateTypeCacheClone()
		{
			var typesByAssemblyQualifiedName = new Dictionary<string, TypeDescription>(_typesByAssemblyQualifiedName.Count);
			foreach (var pair in _typesByAssemblyQualifiedName)
				typesByAssemblyQualifiedName.Add(pair.Key, pair.Value);
			return typesByAssemblyQualifiedName;
		}

		/// <summary>
		///     Tests if the given type has been added to this model.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		[Pure]
		public bool Contains<T>()
		{
			return Contains(typeof(T));
		}

		/// <summary>
		///     Tests if the given type has been added to this model.
		/// </summary>
		/// <param name="type"></param>
		[Pure]
		public bool Contains(Type type)
		{
			if (type == null)
				return false;

			var name = type.AssemblyQualifiedName;
			if (name == null)
				return false;

			return _typesByAssemblyQualifiedName.ContainsKey(name);
		}

		private int GetNextId()
		{
			return _nextId++;
		}

		/// <summary>
		///     This method is called right after deserialization and restores all references in this type
		///     model (this is only necessary because the <see cref="ISerializer" /> implementation only
		///     works with directed acyclic graphs and a type model, naturally, contains cycles).
		/// </summary>
		[AfterDeserialize]
		public void AfterDeserialize()
		{
			if (_types != null)
			{
				var typesById = new Dictionary<int, TypeDescription>(_types.Count);
				foreach (var type in _types)
				{
					var id = type.Id;
					TypeDescription previousType;
					if (!typesById.TryGetValue(id, out previousType))
						typesById.Add(id, type);
					else
						Log.WarnFormat(
						               "Found two types '{0}' and '{1}' which both claim to have the same id '{2}'. Ignoring the former type...",
						               type,
						               previousType,
						               id);

					var assemblyQualifiedName = type.AssemblyQualifiedName;
					if (assemblyQualifiedName != null)
						if (!_typesByAssemblyQualifiedName.TryGetValue(assemblyQualifiedName, out previousType))
							_typesByAssemblyQualifiedName.Add(assemblyQualifiedName, type);
						else
							Log.WarnFormat(
							               "Found two types '{0}' and '{1}' which both claim to have the same assembly qualified name '{2}'. Ignoring the former type...",
							               type, previousType, assemblyQualifiedName);
					else
						Log.WarnFormat("Did not expect the assembly qualified name of the given type to be null: {0}", type);
				}

				foreach (var type in _types)
				{
					foreach (var field in type.Fields)
					{
						TypeDescription fieldType;
						if (typesById.TryGetValue(field.FieldTypeId, out fieldType))
							field.FieldType = fieldType;
					}

					foreach (var property in type.Properties)
					{
						TypeDescription propertyType;
						if (typesById.TryGetValue(property.PropertyTypeId, out propertyType))
							property.PropertyType = propertyType;
					}
				}
			}
		}
	}
}