using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlSerializationCompiler
		: ISerializationMethodCompiler<XmlMethodsCompiler>
	{
		private readonly ModuleBuilder _module;

		public XmlSerializationCompiler(ModuleBuilder moduleBuilder)
		{
			_module = moduleBuilder;
		}

		public XmlMethodsCompiler Prepare(string typeName, TypeDescription typeDescription)
		{
			TypeBuilder typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			return XmlMethodsCompiler.Create(typeBuilder, typeDescription);
		}

		public void Compile(XmlMethodsCompiler methods, ISerializationMethodStorage<XmlMethodsCompiler> storage)
		{
			methods.Compile(storage);
		}
	}
}