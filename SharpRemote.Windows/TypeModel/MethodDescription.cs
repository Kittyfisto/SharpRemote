using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="MethodInfo" /> (in that it describes a particular .NET method), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class MethodDescription
		: IMethodDescription
	{
		/// <inheritdoc />
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		///     The equivalent of <see cref="MethodInfo.ReturnParameter" />.
		/// </summary>
		[DataMember]
		public ParameterDescription ReturnParameter { get; set; }

		/// <summary>
		///     The equivalent of <see cref="MethodInfo.ReturnType" />.
		/// </summary>
		public TypeDescription ReturnType => ReturnParameter?.ParameterType;

		/// <summary>
		///     The equivalent of <see cref="MethodBase.GetParameters" />.
		/// </summary>
		[DataMember]
		public ParameterDescription[] Parameters { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0} {1}({2})", ReturnParameter, Name, string.Join(", ", (IEnumerable<ParameterDescription>)Parameters));
		}

		IParameterDescription IMethodDescription.ReturnParameter => ReturnParameter;
		ITypeDescription IMethodDescription.ReturnType => ReturnType;
		IReadOnlyList<IParameterDescription> IMethodDescription.Parameters => Parameters;
	}
}