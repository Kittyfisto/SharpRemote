using System;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class SingletonWithBeforeSerializeCallback
	{
		[SingletonFactoryMethod]
		public static SingletonWithBeforeSerializeCallback SingletonFactoryMethod()
		{
			throw new NotImplementedException();
		}

		[BeforeSerialize]
		public void BeforeSerialize()
		{

		}
	}
}