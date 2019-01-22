// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	/// The signature of a method is incompatible because of any combination of the following:
	/// - The return type has been changed (i.e. int => long)
	/// - The number of parameters is different (i.e. (int, string) to (int, string, byte))
	/// - A parameter type has changed (i.e. int => uint)
	/// </summary>
	internal class IncompatibleMethodSignature
		: ITypeModelDifference
	{

	}
}