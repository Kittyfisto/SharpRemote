using System;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class SingletonWithAfterDeserializeCallback
	{
		[SingletonFactoryMethod]
		public static SingletonWithAfterDeserializeCallback SingletonFactoryMethod()
		{
			throw new NotImplementedException();
		}

		[AfterDeserialize]
		public void AfterDeserialize()
		{

		}
	}
}