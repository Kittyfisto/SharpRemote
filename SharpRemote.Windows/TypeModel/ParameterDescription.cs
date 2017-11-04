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

		/// <summary>
		///     The id of the <see cref="ParameterType" />.
		/// </summary>
		[DataMember]
		public int ParameterTypeId { get; set; }

		/// <summary>
		///     The equivalent of <see cref="ParameterInfo.ParameterType" />.
		/// </summary>
		public TypeDescription ParameterType
		{
			get { return _parameterType; }
			set
			{
				_parameterType = value;
				ParameterTypeId = value?.Id ?? 0;
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

		ITypeDescription IParameterDescription.ParameterType => _parameterType;

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0} {1}", _parameterType, Name);
		}
	}
}