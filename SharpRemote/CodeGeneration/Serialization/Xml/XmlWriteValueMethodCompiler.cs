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
		private static readonly MethodInfo XmlSerializerWriteDecimal;
		private static readonly MethodInfo XmlSerializerWriteString;
		private static readonly MethodInfo XmlSerializerWriteByte;
		private static readonly MethodInfo XmlSerializerWriteSByte;
		private static readonly MethodInfo XmlSerializerWriteInt16;
		private static readonly MethodInfo XmlSerializerWriteUInt16;
		private static readonly MethodInfo XmlSerializerWriteInt32;
		private static readonly MethodInfo XmlSerializerWriteUInt32;
		private static readonly MethodInfo XmlSerializerWriteInt64;
		private static readonly MethodInfo XmlSerializerWriteUInt64;
		private static readonly MethodInfo XmlSerializerWriteSingle;
		private static readonly MethodInfo XmlSerializerWriteDouble;
		private static readonly MethodInfo XmlSerializerWriteDateTime;
		private static readonly MethodInfo XmlSerializerWriteException;

		static XmlWriteValueMethodCompiler()
		{
			XmlWriterWriteStartElement = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteStartElement), new [] {typeof(string)});
			XmlWriterWriteEndElement = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteEndElement));
			XmlWriterWriteStringValue = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteValue), new[] {typeof(string)});
			XmlWriterWriteAttributeString = typeof(XmlWriter).GetMethod(nameof(XmlWriter.WriteAttributeString), new [] {typeof(string), typeof(string)});
			XmlSerializerWriteDecimal = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new [] {typeof(XmlWriter), typeof(decimal)});
			XmlSerializerWriteString = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(string) });
			XmlSerializerWriteByte = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] {typeof(XmlWriter), typeof(byte)});
			XmlSerializerWriteSByte = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(sbyte) });
			XmlSerializerWriteInt16 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(short) });
			XmlSerializerWriteUInt16 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(ushort) });
			XmlSerializerWriteInt32 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(int) });
			XmlSerializerWriteUInt32 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(uint) });
			XmlSerializerWriteInt64 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(long) });
			XmlSerializerWriteUInt64 = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(ulong) });
			XmlSerializerWriteSingle = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(float) });
			XmlSerializerWriteDouble = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new[] { typeof(XmlWriter), typeof(double) });
			XmlSerializerWriteDateTime = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteValue), new [] {typeof(XmlWriter), typeof(DateTime)});
			XmlSerializerWriteException = typeof(XmlSerializer).GetMethod(nameof(XmlSerializer.WriteException), new [] {typeof(XmlWriter), typeof(XmlSerializer), typeof(Exception)});
		}

		public XmlWriteValueMethodCompiler(CompilationContext context) : base(context)
		{}

		protected override void EmitWriteHint(ILGenerator generator, ByReferenceHint hint)
		{ }

		protected override void EmitBeginWriteField(ILGenerator gen, FieldDescription field)
		{
			// XmlWriter.WriteStartElement("Field")
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.FieldElementName);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteStartElement);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.NameAttributeName);
			gen.Emit(OpCodes.Ldstr, field.Name);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteAttributeString);
		}

		protected override void EmitEndWriteField(ILGenerator gen, FieldDescription field)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteEndElement);
		}

		protected override void EmitBeginWriteProperty(ILGenerator gen, PropertyDescription property)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.PropertyElementName);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteStartElement);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldstr, XmlSerializer.NameAttributeName);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteAttributeString);
		}

		protected override void EmitEndWriteProperty(ILGenerator gen, PropertyDescription property)
		{
			// XmlWriter.WriteEndElement()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, XmlWriterWriteEndElement);
		}

		protected override void EmitWriteByte(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteByte);
		}

		protected override void EmitWriteSByte(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteSByte);
		}

		protected override void EmitWriteUInt16(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteUInt16);
		}

		protected override void EmitWriteInt16(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteInt16);
		}

		protected override void EmitWriteUInt32(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteUInt32);
		}

		protected override void EmitWriteInt32(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteInt32);
		}

		protected override void EmitWriteUInt64(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteUInt64);
		}

		protected override void EmitWriteInt64(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteInt64);
		}

		protected override void EmitWriteDecimal(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			// XmlSerializer.WriteValue(decimal)
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteDecimal);
		}

		protected override void EmitWriteSingle(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			// XmlSerializer.WriteValue(decimal)
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteSingle);
		}

		protected override void EmitWriteDouble(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			// XmlSerializer.WriteValue(decimal)
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteDouble);
		}

		protected override void EmitWriteString(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteString);
		}

		protected override void EmitWriteDateTime(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			gen.Emit(OpCodes.Ldarg_0);
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteDateTime);
		}

		protected override void EmitWriteLevel(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			// TODO: How can we avoid inlining this method call?
			// There's just no point in duplicating that many instructions...

			var end = gen.DefineLabel();

			for (int i = 0; i < HardcodedLevels.Count; ++i)
			{
				var next = gen.DefineLabel();

				// if (ReferenceEquals(value, <fld>))
				loadMember();
				gen.Emit(OpCodes.Ldsfld, HardcodedLevels[i].Field);
				gen.Emit(OpCodes.Call, Methods.ObjectReferenceEquals);
				gen.Emit(OpCodes.Brfalse, next);

				// writer.WriteByte(<constant>)
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldstr, "SpecialValue");
				gen.Emit(OpCodes.Ldstr, HardcodedLevels[i].Name);
				gen.Emit(OpCodes.Callvirt, XmlWriterWriteAttributeString);
				gen.Emit(OpCodes.Br, end);

				gen.MarkLabel(next);
			}

			gen.MarkLabel(end);
		}

		protected override void EmitWriteException(ILGenerator gen, Action loadMember, Action loadMemberAddress)
		{
			// push XmlWriter
			gen.Emit(OpCodes.Ldarg_0);
			// push XmlSerializer
			gen.Emit(OpCodes.Ldarg_2);
			// push exception
			loadMember();
			gen.Emit(OpCodes.Call, XmlSerializerWriteException);
		}

		protected override void EmitWriteObjectId(ILGenerator generator, LocalBuilder proxy)
		{}
	}
}