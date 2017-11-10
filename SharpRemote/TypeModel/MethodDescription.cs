using System.Collections.Generic;
using System.Linq;
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
		private readonly MethodInfo _method;

		/// <summary>
		/// </summary>
		public MethodDescription()
		{
		}

		private MethodDescription(MethodInfo methodInfo)
		{
			Name = methodInfo.Name;
			_method = methodInfo;
		}

		/// <summary>
		/// </summary>
		public MethodInfo Method => _method;

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
		[DataMember]
		public string Name { get; set; }

		IParameterDescription IMethodDescription.ReturnParameter => ReturnParameter;
		ITypeDescription IMethodDescription.ReturnType => ReturnType;
		IReadOnlyList<IParameterDescription> IMethodDescription.Parameters => Parameters;

		/// <inheritdoc />
		public override string ToString()
		{
			var parameters = Parameters ?? Enumerable.Empty<ParameterDescription>();
			return string.Format("{0} {1}({2})", ReturnParameter, Name, string.Join(", ", parameters));
		}

		/// <summary>
		/// </summary>
		/// <param name="methodInfo"></param>
		/// <param name="typesByAssemblyQualifiedName"></param>
		/// <returns></returns>
		public static MethodDescription Create(MethodInfo methodInfo,
		                                       IDictionary<string, TypeDescription> typesByAssemblyQualifiedName)
		{
			var description = new MethodDescription(methodInfo)
			{
				ReturnParameter = ParameterDescription.Create(methodInfo.ReturnParameter, typesByAssemblyQualifiedName)
			};
			return description;
		}
	}
}