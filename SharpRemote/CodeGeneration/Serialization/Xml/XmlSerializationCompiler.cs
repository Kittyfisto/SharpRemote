using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	sealed class XmlSerializationCompiler
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
			return new XmlMethodsCompiler(typeBuilder, typeDescription);
		}

		public void Compile(XmlMethodsCompiler methodses, ISerializationMethodStorage<XmlMethodsCompiler> storage)
		{
			methodses.Compile(storage);
		}
	}
}