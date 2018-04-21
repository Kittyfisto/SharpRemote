namespace SharpRemote.CodeGeneration.Serialization
{
	internal interface ISerializationMethodCompiler<T>
		where T : ISerializationMethods
	{
		/// <summary>
		///     Creates a new store for serialization and deserialization of the given <paramref name="typeDescription" />.
		///     THIS SHALL NOT COMPILE ANYTHING BUT MERELY PREPARE A NEW CONTAINER FOR LATER COMPILATION.
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="typeDescription"></param>
		/// <returns></returns>
		T Prepare(string typeName, ITypeDescription typeDescription);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="methods"></param>
		/// <param name="storage"></param>
		void Compile(T methods, ISerializationMethodStorage<T> storage);
	}
}