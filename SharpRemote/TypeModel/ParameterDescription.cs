using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="ParameterInfo" /> (in that it describes a particular .NET method), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class ParameterDescription
		: IParameterDescription
	{
		private TypeDescription _parameterType;
		private int? _parameterTypeId;

		/// <summary>
		///     The id of the <see cref="ParameterType" />.
		/// </summary>
		[DataMember]
		public int ParameterTypeId
		{
			get { return _parameterTypeId ?? _parameterType?.Id ?? 0; }
			set { _parameterTypeId = value; }
		}

		/// <summary>
		///     The equivalent of <see cref="ParameterInfo.ParameterType" />.
		/// </summary>
		public TypeDescription ParameterType
		{
			get { return _parameterType; }
			set
			{
				_parameterType = value;
				if (value != null)
				{
					var id = value.Id;
					if (id > 0)
						_parameterTypeId = id;
				}
			}
		}

		/// <inheritdoc />
		[DataMember]
		public string Name { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsIn { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsOut { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsRetval { get; set; }

		/// <inheritdoc />
		[DataMember]
		public int Position { get; set; }

		ITypeDescription IParameterDescription.ParameterType
		{
			get { return _parameterType; }
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0} {1}", _parameterType, Name);
		}

		/// <summary>
		///     Creates a new parameter description for the given type.
		/// </summary>
		/// <param name="parameter"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
		public static ParameterDescription Create(ParameterInfo parameter,
		                                          IDictionary<string, TypeDescription> typesByAssemblyQualifiedName)
		{
			var description = new ParameterDescription
			{
				Name = parameter.Name,
				IsIn = parameter.IsIn,
				IsOut = parameter.IsOut,
				IsRetval = parameter.IsRetval,
				Position = parameter.Position,
				ParameterType = TypeDescription.GetOrCreate(parameter.ParameterType, typesByAssemblyQualifiedName)
			};

			return description;
		}

		internal IEnumerable<ITypeModelDifference> FindDifferences(ParameterDescription actualParameter)
		{
			var differences = new List<ITypeModelDifference>();
			if (IsIn != actualParameter.IsIn)
				differences.Add(new IncompatibleMethodSignature());
			if (IsOut != actualParameter.IsOut)
				differences.Add(new IncompatibleMethodSignature());
			if (IsRetval != actualParameter.IsRetval)
				differences.Add(new IncompatibleMethodSignature());
			if (ParameterType.Type != actualParameter.ParameterType.Type)
				differences.Add(new ParameterTypeMismatch(this, actualParameter));
			return differences;
		}
	}
}