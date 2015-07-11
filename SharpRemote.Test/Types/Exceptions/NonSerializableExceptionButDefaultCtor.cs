using System;

namespace SharpRemote.Test.Types.Exceptions
{
	/// <summary>
	///     An exception that does not implement the exception-serialization pattern as advocated on http://stackoverflow.com/questions/94488/what-is-the-correct-way-to-make-a-custom-net-exception-serializable.
	///     However it does offer a default ctor and thus could be rethrown on the caller's side.
	/// </summary>
	public sealed class NonSerializableExceptionButDefaultCtor
		: Exception
	{
	}
}