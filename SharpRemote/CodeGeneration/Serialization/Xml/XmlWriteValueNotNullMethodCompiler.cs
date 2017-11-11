using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlWriteValueNotNullMethodCompiler
		: AbstractWriteValueNotNullMethodCompiler
	{
		private static readonly MethodInfo XmlWriterWriteStartElement;
		private static readonly MethodInfo XmlWriterWriteEndElement;
		private static readonly MethodInfo XmlWriterWriteStringValue;
		private static readonly MethodInfo DecimalToString;

		static XmlWriteValueNotNullMethodCompiler()
		{
			XmlWriterWriteStartElement = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteStartElement), new [] {typeof(string)});
			XmlWriterWriteEndElement = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteEndElement));
			XmlWriterWriteStringValue = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteValue), new[] {typeof(string)});
			DecimalToString = typeof(decimal).GetMethod(nameof(decimal.ToString), new [] {typeof(IFormatProvider)});
		}

		public XmlWriteValueNotNullMethodCompiler(CompilationContext context) : base(context)
		{
		}

		protected override void EmitWriteHint(ILGenerator generator, ByReferenceHint hint)
		{ }

		protected override void EmitBeginWriteFieldOrProperty(ILGenerator generator, TypeDescription valueType, string name)
		{
			// XmlWriter.WriteStartElement(value)
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldstr, name);
			generator.Emit(OpCodes.Callvirt, XmlWriterWriteStartElement);
		}

		protected override void EmitEndWriteFieldOrProperty(ILGenerator generator, TypeDescription valueType, string name)
		{
			// XmlWriter.WriteEndElement()
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, XmlWriterWriteEndElement);
			generator.Emit(OpCodes.Nop);
		}

		protected override void EmitWriteByte(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteSByte(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteUShort(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteShort(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteUInt(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteInt(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteULong(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteLong(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteDecimal(ILGenerator gen, Action loadValue)
		{
			// XmlWriter.WriteValue(value.ToString(CultureInfo.InvariantCulture));
			gen.Emit(OpCodes.Ldarg_0);
			loadValue();
			gen.Emit(OpCodes.Call, CultureInfoGetInvariantCulture);
			gen.Emit(OpCodes.Call, DecimalToString);
			gen.Emit(OpCodes.Call, XmlWriterWriteStringValue);
		}

		protected override void EmitWriteFloat(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteDouble(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteString(ILGenerator gen, Action loadValue)
		{
			throw new NotImplementedException();
		}

		protected override void EmitWriteObjectId(ILGenerator generator, LocalBuilder proxy)
		{}
	}
}