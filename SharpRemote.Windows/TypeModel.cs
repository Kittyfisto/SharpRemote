using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using SharpRemote.Attributes;

namespace SharpRemote
{
	/// <summary>
	///     A representation of all types registered with a <see cref="ISerializer" />.
	///     This representation describes each type (as far as serialization is concerned) and
	///     may be serialized/deserialized *without* requiring that the types describes by this type model
	///     can be loaded.
	/// </summary>
	public sealed class TypeModel
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly List<TypeDescription> _types;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		public TypeModel()
		{
			_types = new List<TypeDescription>();
		}

		/// <summary>
		///     The types added to this model.
		/// </summary>
		public IReadOnlyList<TypeDescription> Types => _types;

		/// <summary>
		///     Adds the given type to this model.
		/// </summary>
		/// <param name="type"></param>
		/// <exception cref="ArgumentNullException">When <paramref name="type" /> is null</exception>
		public void Add(TypeDescription type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			_types.Add(type);
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
						Log.WarnFormat("Found two types '{0}' and '{1}' which both claim to have the same id '{2}'. Ignoring the former type...",
							type,
							previousType,
							id);
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