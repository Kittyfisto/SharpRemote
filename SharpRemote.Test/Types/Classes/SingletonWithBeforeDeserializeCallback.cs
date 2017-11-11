using System;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class SingletonWithBeforeDeserializeCallback
	{
		[SingletonFactoryMethod]
		public static SingletonWithBeforeDeserializeCallback SingletonFactoryMethod()
		{
			throw new NotImplementedException();
		}

		[BeforeDeserialize]
		public void BeforeDeserialize()
		{

		}
	}
}