using System;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	/// <summary>
	///     This is an example of what not to do:
	///     ByReference and Singleton are very different concepts (as far as remoting is concerned)
	///     and they cannot be married:
	///     ByReference implies that a type isn't serialized at all and all access generates additional
	///     remote procedure calls.
	///     Singleton implies that a type may only exist once per AppDomain and thus when such a type
	///     is sent over the wire, each EndPoint re-uses that instance.
	/// </summary>
	public sealed class SingletonByReference
		: IByReferenceType
	{
		private static readonly SingletonByReference _instance;

		static SingletonByReference()
		{
			_instance = new SingletonByReference();
		}

		private SingletonByReference()
		{
		}

		[SingletonFactoryMethod]
		public static SingletonByReference Instance => _instance;

		public int Value
		{
			get { throw new NotImplementedException(); }
		}
	}
}