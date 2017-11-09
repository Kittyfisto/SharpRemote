using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	sealed class XmlSerializationCompiler
		: ISerializationMethodCompiler<XmlSerializationMethods>
	{
		private readonly ModuleBuilder _module;

		public XmlSerializationCompiler(ModuleBuilder moduleBuilder)
		{
			_module = moduleBuilder;
		}

		public XmlSerializationMethods Prepare(string typeName, TypeDescription typeDescription)
		{
			TypeBuilder typeBuilder = _module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
			return new XmlSerializationMethods(typeBuilder, typeDescription);
		}

		public void Compile(XmlSerializationMethods methods, ISerializationMethodStorage<XmlSerializationMethods> storage)
		{
			methods.Compile(storage);
		}
	}
}