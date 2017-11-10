using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	sealed class XmlSerializationCompiler
		: ISerializationMethodCompiler<XmlMethodCompiler>
	{
		private readonly ModuleBuilder _module;

		public XmlSerializationCompiler(ModuleBuilder moduleBuilder)
		{
			_module = moduleBuilder;
		}

		public XmlMethodCompiler Prepare(string typeName, TypeDescription typeDescription)
		{
			TypeBuilder typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			return new XmlMethodCompiler(typeBuilder, typeDescription);
		}

		public void Compile(XmlMethodCompiler methods, ISerializationMethodStorage<XmlMethodCompiler> storage)
		{
			methods.Compile(storage);
		}
	}
}