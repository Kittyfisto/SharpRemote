using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlWriteValueMethodCompiler
		: AbstractWriteValueMethodCompiler
	{
		private static readonly MethodInfo XmlWriterWriteStartElement;
		private static readonly MethodInfo XmlWriterWriteEndElement;
		private static readonly MethodInfo XmlWriterWriteStringValue;
		private static readonly MethodInfo XmlWriterWriteAttributeString;
		private static readonly MethodInfo DecimalToString;

		public const string FieldElementName = "Field";
		public const string PropertyElementName = "Property";
		public const string NameAttributeName = "Name";

		static XmlWriteValueMethodCompiler()
		{
			XmlWriterWriteStartElement = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteStartElement), new [] {typeof(string)});
			XmlWriterWriteEndElement = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteEndElement));
			XmlWriterWriteStringValue = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteValue), new[] {typeof(string)});
			XmlWriterWriteAttributeString = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteAttributeString), new [] {typeof(string), typeof(string)});
			DecimalToString = typeof(decimal).GetMethod(nameof(decimal.ToString), new [] {typeof(IFormatProvider)});
		}

		public XmlWriteValueMethodCompiler(CompilationContext context) : base(context)
		{
		}

		protected override void EmitWriteHint(ILGenerator generator, ByReferenceHint hint)
		{ }

		protected override void EmitBeginWriteField(ILGenerator gen, FieldDescription field, string fieldName)
		{
			// XmlWriter.WriteStartElement("Field")
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, FieldElementName);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteStartElement);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, NameAttributeName);
			gen.Emit(OpCodes.Ldstr, fieldName);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteAttributeString);
		}

		protected override void EmitEndWriteField(ILGenerator gen, FieldDescription field, string fieldName)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteEndElement);
		}

		protected override void EmitBeginWriteProperty(ILGenerator gen, PropertyDescription property, string propertyName)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, PropertyElementName);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteStartElement);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, NameAttributeName);
			gen.Emit(OpCodes.Ldstr, propertyName);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteAttributeString);
		}

		protected override void EmitEndWriterProperty(ILGenerator gen, PropertyDescription property, string propertyName)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteEndElement);
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