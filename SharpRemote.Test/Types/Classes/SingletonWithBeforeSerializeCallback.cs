using System;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class SingletonWithBeforeSerializeCallback
	{
		[SingletonFactoryMethod]
		public static SingletonWithBeforeSerializeCallback SingletonFactoryMethod()
		{
			throw new NotImplementedException();
		}

		[BeforeSerialize]
		public static void BeforeSerialize()
		{

		}
	}
}