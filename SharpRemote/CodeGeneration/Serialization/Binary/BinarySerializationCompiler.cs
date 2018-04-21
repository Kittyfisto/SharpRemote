using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinarySerializationCompiler
		: ISerializationMethodCompiler<BinaryMethodsCompiler>
	{
		private readonly ModuleBuilder _module;

		public BinarySerializationCompiler(ModuleBuilder moduleBuilder)
		{
			_module = moduleBuilder;
		}

		public BinaryMethodsCompiler Prepare(string typeName, ITypeDescription typeDescription)
		{
			TypeBuilder typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			return BinaryMethodsCompiler.Create(typeBuilder, typeDescription);
		}

		public void Compile(BinaryMethodsCompiler methods, ISerializationMethodStorage<BinaryMethodsCompiler> storage)
		{
			methods.Compile(storage);
		}
	}
}