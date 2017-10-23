using System;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class SingletonWithBeforeDeserializeCallback
	{
		[SingletonFactoryMethod]
		public static SingletonWithBeforeDeserializeCallback SingletonFactoryMethod()
		{
			throw new NotImplementedException();
		}

		[BeforeDeserialize]
		public static void BeforeDeserialize()
		{

		}
	}
}