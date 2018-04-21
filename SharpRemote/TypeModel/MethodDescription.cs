using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using SharpRemote.Attributes;

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
		private static readonly ParameterDescription[] EmptyParameters = new ParameterDescription[0];

		private readonly MethodInfo _method;
		private readonly SpecialMethod _specialMethod;

		/// <summary>
		/// </summary>
		public MethodDescription()
		{
			Parameters = EmptyParameters;
		}

		private MethodDescription(MethodInfo method)
			: this()
		{
			Name = method.Name;
			_method = method;

			var returnType = method.ReturnType;
			if (returnType == typeof(Task) ||
			    (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)))
			{
				IsAsync = true;
			}

			var type = method.DeclaringType;
			if (IsSerializationCallback(method, out _specialMethod))
			{
				if (type.IsValueType)
					throw new ArgumentException(
					                            string.Format(
					                                          "The type '{0}.{1}' may not contain methods marked with the [{2}] attribute: Only classes may have these callbacks",
					                                          type.Namespace, type.Name, _specialMethod));

				if (!method.IsPublic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The method '{0}.{1}.{2}()' is marked with the [{3}] attribute and must therefore be publicly accessible",
					                                          type.Namespace, type.Name, method.Name,
					                                          _specialMethod));

				if (method.IsStatic)
					throw new ArgumentException(
					                            string.Format(
					                                          "The method '{0}.{1}.{2}()' is marked with the [{3}] attribute and must therefore be non-static",
					                                          type.Namespace, type.Name, method.Name,
					                                          _specialMethod));

				var parameters = method.GetParameters();
				if (parameters.Length > 0)
					throw new ArgumentException(
					                            string.Format(
					                                          "The method '{0}.{1}.{2}()' is marked with the [{3}] attribute and must therefore be parameterless",
					                                          type.Namespace, type.Name, method.Name,
					                                          _specialMethod));

				if (method.IsGenericMethodDefinition)
					throw new ArgumentException(
					                            string.Format(
					                                          "The method '{0}.{1}.{2}()' is marked with the [{3}] attribute and must therefore be non-generic",
					                                          type.Namespace, type.Name, method.Name,
					                                          _specialMethod));
			}
		}

		/// <inheritdoc />
		public MethodInfo Method => _method;

		/// <inheritdoc />
		[DataMember]
		public bool IsAsync { get; set; }

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

		/// <summary>
		/// </summary>
		public SpecialMethod SpecialMethod => _specialMethod;

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
				ReturnParameter = ParameterDescription.Create(methodInfo.ReturnParameter, typesByAssemblyQualifiedName),
			};
			var parameters = methodInfo.GetParameters();
			if (parameters.Length > 0)
			{
				description.Parameters =
					parameters.Select(x => ParameterDescription.Create(x, typesByAssemblyQualifiedName)).ToArray();
			}
			else
			{
				description.Parameters = EmptyParameters;
			}

			return description;
		}

		[Pure]
		private static string StripAttribute(string attributeTypeName)
		{
			const string attr = "Attribute";
			if (attributeTypeName.EndsWith(attr))
				return attributeTypeName.Substring(startIndex: 0, length: attributeTypeName.Length - attr.Length);

			return attributeTypeName;
		}

		[Pure]
		private static bool IsSerializationCallback(MethodInfo method, out SpecialMethod specialMethod)
		{
			var beforeSerialize = method.GetCustomAttribute<BeforeSerializeAttribute>();
			var afterSerialize = method.GetCustomAttribute<AfterSerializeAttribute>();
			var beforeDeserialize = method.GetCustomAttribute<BeforeDeserializeAttribute>();
			var afterDeserialize = method.GetCustomAttribute<AfterDeserializeAttribute>();

			if (beforeSerialize != null)
			{
				specialMethod = SpecialMethod.BeforeSerialize;
				return true;
			}

			if (afterSerialize != null)
			{
				specialMethod = SpecialMethod.AfterSerialize;
				return true;
			}

			if (beforeDeserialize != null)
			{
				specialMethod = SpecialMethod.BeforeDeserialize;
				return true;
			}

			if (afterDeserialize != null)
			{
				specialMethod = SpecialMethod.AfterDeserialize;
				return true;
			}

			specialMethod = SpecialMethod.None;
			return false;
		}
	}
}