using System;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class TypeModelComparer
	{
		/// <summary>
		///    Verifies if the given (remote) type model implements the expected interface <typeparamref name="TInterface"/>.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <param name="remoteTypeModel"></param>
		/// <returns>True if the type model implements the interface and supports all methods offered by that interface, false otherwise</returns>
		public static bool IsCompatible<TInterface>(TypeModel remoteTypeModel) where TInterface : class
		{
			return IsCompatible(remoteTypeModel, typeof(TInterface));
		}

		/// <summary>
		///    Verifies if the given (remote) type model implements the expected interface <paramref name="expectedInterface"/>.
		/// </summary>
		/// <param name="remoteTypeModel"></param>
		/// <param name="expectedInterface"></param>
		/// <returns>True if the type model implements the interface and supports all methods offered by that interface, false otherwise</returns>
		public static bool IsCompatible(TypeModel remoteTypeModel, Type expectedInterface)
		{
			var expectedTypeModel = new TypeModel();
			expectedTypeModel.Add(expectedInterface);

			var differences = expectedTypeModel.FindDifferences(remoteTypeModel);
			return !differences.Any();
		}
	}
}
