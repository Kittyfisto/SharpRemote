using System;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class SingletonWithAfterSerializeCallback
	{
		[SingletonFactoryMethod]
		public static SingletonWithAfterSerializeCallback SingletonFactoryMethod()
		{
			throw new NotImplementedException();
		}

		[AfterSerialize]
		public void AfterSerialize()
		{

		}
	}
}