using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public class ProxyCompiler
		: Compiler
	{
		private readonly Type _interfaceType;
		private readonly ModuleBuilder _module;
		private readonly TypeBuilder _typeBuilder;
		private readonly Dictionary<string, FieldBuilder> _fields;

		#region Methods

		#endregion

		public ProxyCompiler(Serializer serializer, ModuleBuilder module, string proxyTypeName, Type interfaceType)
			: base(serializer)
		{
			if (module == null) throw new ArgumentNullException("module");
			if (proxyTypeName == null) throw new ArgumentNullException("proxyTypeName");
			if (interfaceType == null) throw new ArgumentNullException("interfaceType");

			_interfaceType = interfaceType;
			_module = module;

			_typeBuilder = _module.DefineType(proxyTypeName, TypeAttributes.Class, typeof (object), new[]
				{
					interfaceType
				});
			_typeBuilder.AddInterfaceImplementation(typeof(IProxy));

			ObjectId = _typeBuilder.DefineField("_objectId", typeof (ulong), FieldAttributes.Private | FieldAttributes.InitOnly);
			EndPoint = _typeBuilder.DefineField("_endPoint", typeof (IRemotingEndPoint),
			                                    FieldAttributes.Private | FieldAttributes.InitOnly);
			Channel = _typeBuilder.DefineField("_channel", typeof (IEndPointChannel),
			                                    FieldAttributes.Private | FieldAttributes.InitOnly);
			Serializer = _typeBuilder.DefineField("_serializer", typeof(ISerializer),
												FieldAttributes.Private | FieldAttributes.InitOnly);
			_fields= new Dictionary<string, FieldBuilder>();
		}

		public Type Generate()
		{
			GenerateCtor();
			GenerateGetObjectId();
			GenerateGetSerializer();
			GenerateMethods();
			GenerateInvokeEvent();

			var proxyType = _typeBuilder.CreateType();
			return proxyType;
		}

		private void GenerateCtor()
		{
			var builder = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[]
				{
					typeof (ulong),
					typeof (IRemotingEndPoint),
					typeof (IEndPointChannel),
					typeof (ISerializer)
				});

			var gen = builder.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ObjectCtor);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, ObjectId);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Stfld, EndPoint);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Stfld, Channel);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg, 4);
			gen.Emit(OpCodes.Stfld, Serializer);

			gen.Emit(OpCodes.Ret);
		}

		private void GenerateGetSerializer()
		{
			var method = _typeBuilder.DefineMethod("get_Serializer", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof(ISerializer), null);
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, Serializer);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetSerializer);
		}

		private void GenerateGetObjectId()
		{
			var method = _typeBuilder.DefineMethod("get_Id", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof (ulong), null);
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, ObjectId);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetObjectId);
		}

		private void GenerateMethods()
		{
			var allMethods = _interfaceType
				.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
				.Concat(_interfaceType.GetInterfaces().SelectMany(x => x.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)))
				.OrderBy(x => x.Name)
				.ToArray();
			foreach (var method in allMethods)
			{
				if (method.IsSpecialName)
				{
					var methodName = method.Name;
					if (methodName.StartsWith("add_"))
					{
						GenerateAddEvent(method);
					}
					else if (methodName.StartsWith("remove_"))
					{
						GenerateRemoveEvent(method);
					}
					else
					{
						GenerateMethodInvocation(method);
					}
				}
				else
				{
					GenerateMethodInvocation(method);
				}
			}
		}

		private void GenerateAddEvent(MethodInfo originalMethod)
		{
			var delegateType = originalMethod.GetParameters().First().ParameterType;
			var method = _typeBuilder.DefineMethod(originalMethod.Name,
			                                       MethodAttributes.Public | MethodAttributes.Virtual |
			                                       MethodAttributes.SpecialName,
			                                       typeof (void),
			                                       new[] {delegateType});

			var fieldName = EventBackingFieldName(originalMethod.Name);
			var backingField = _typeBuilder.DefineField(fieldName, delegateType, FieldAttributes.Private);
			_fields.Add(fieldName, backingField);

			var gen = method.GetILGenerator();
			var l0 = gen.DeclareLocal(delegateType);
			var l1 = gen.DeclareLocal(delegateType);
			var l2 = gen.DeclareLocal(delegateType);
			var l3 = gen.DeclareLocal(typeof(bool));
			var startAllOver = gen.DefineLabel();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, backingField);
			gen.Emit(OpCodes.Stloc, l0);
			gen.MarkLabel(startAllOver);
			gen.Emit(OpCodes.Ldloc, l0);
			gen.Emit(OpCodes.Stloc, l1);
			gen.Emit(OpCodes.Ldloc, l1);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, Methods.DelegateCombine);
			gen.Emit(OpCodes.Castclass, delegateType);
			gen.Emit(OpCodes.Stloc, l2);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldflda, backingField);
			gen.Emit(OpCodes.Ldloc, l2);
			gen.Emit(OpCodes.Ldloc, l1);

			var compareExchange = Methods.InterlockedCompareExchangeGeneric.MakeGenericMethod(delegateType);
			gen.Emit(OpCodes.Call, compareExchange);

			gen.Emit(OpCodes.Stloc, l0);
			gen.Emit(OpCodes.Ldloc, l0);
			gen.Emit(OpCodes.Ldloc, l1);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, l3);
			gen.Emit(OpCodes.Ldloc, l3);
			gen.Emit(OpCodes.Brtrue, startAllOver);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, originalMethod);
		}

		private void GenerateRemoveEvent(MethodInfo originalMethod)
		{
			var delegateType = originalMethod.GetParameters().First().ParameterType;
			var method = _typeBuilder.DefineMethod(originalMethod.Name,
												   MethodAttributes.Public | MethodAttributes.Virtual |
												   MethodAttributes.SpecialName,
												   typeof(void),
												   new[] { delegateType });

			var fieldName = EventBackingFieldName(originalMethod.Name);
			var backingField = _fields[fieldName];

			var gen = method.GetILGenerator();
			gen.DeclareLocal(delegateType);
			gen.DeclareLocal(delegateType);
			gen.DeclareLocal(delegateType);
			gen.DeclareLocal(typeof(bool));
			var startAllOver = gen.DefineLabel();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, backingField);
			gen.Emit(OpCodes.Stloc_0);
			gen.MarkLabel(startAllOver);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, Methods.DelegateRemove);
			gen.Emit(OpCodes.Castclass, delegateType);
			gen.Emit(OpCodes.Stloc_2);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldflda, backingField);
			gen.Emit(OpCodes.Ldloc_2);
			gen.Emit(OpCodes.Ldloc_1);
			var compareExchanged = Methods.InterlockedCompareExchangeGeneric.MakeGenericMethod(delegateType);
			gen.Emit(OpCodes.Call, compareExchanged);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_3);
			gen.Emit(OpCodes.Ldloc_3);
			gen.Emit(OpCodes.Brtrue, startAllOver);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, originalMethod);
		}

		private string EventBackingFieldName(string methodName)
		{
			var builder = new StringBuilder(methodName);
			if (methodName.StartsWith("add_"))
			{
				builder.Remove(0, 4);
			}
			else if (methodName.StartsWith("remove_"))
			{
				builder.Remove(0, 7);
			}
			else
			{
				throw new ArgumentException(string.Format("Can't build field-name for special method: {0}", methodName));
			}

			builder[0] = char.ToLower(builder[0]);
			builder.Insert(0, '_');
			return builder.ToString();
		}

		private void GenerateInvokeEvent()
		{
			var originalMethod = Methods.GrainInvoke;
			var method = _typeBuilder.DefineMethod(originalMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual,
			                                       typeof (void),
			                                       new[]
				                                       {
					                                       typeof (string),
					                                       typeof (BinaryReader),
					                                       typeof (BinaryWriter)
				                                       });

			var gen = method.GetILGenerator();

			var name = gen.DeclareLocal(typeof(string));
			var @throw = gen.DefineLabel();
			var @ret = gen.DefineLabel();

			// if (method == null) goto ret
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stloc, name);
			gen.Emit(OpCodes.Ldloc, name);
			gen.Emit(OpCodes.Brfalse, @throw);

			var allEvents = _interfaceType.GetEvents();
			var labels = new Label[allEvents.Length];
			int index = 0;
			foreach (var eventInfo in allEvents)
			{
				gen.Emit(OpCodes.Ldloc, name);
				gen.Emit(OpCodes.Ldstr, eventInfo.Name);
				gen.Emit(OpCodes.Call, Methods.StringEquality);

				var @true = gen.DefineLabel();
				labels[index++] = @true;
				gen.Emit(OpCodes.Brtrue, @true);
			}

			gen.Emit(OpCodes.Br, @throw);

			for (int i = 0; i < allEvents.Length; ++i)
			{
				var methodInfo = allEvents[i];
				var label = labels[i];

				gen.MarkLabel(label);
				ExtractArgumentsAndInvokeEvent(gen, methodInfo);
				gen.Emit(OpCodes.Br, @ret);
			}

			gen.MarkLabel(@throw);
			gen.Emit(OpCodes.Ldstr, "Event '{0}' not found");
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, Methods.StringFormat);
			gen.Emit(OpCodes.Newobj, Methods.ArgumentExceptionCtor);
			gen.Emit(OpCodes.Throw);

			gen.MarkLabel(@ret);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, Methods.BinaryWriterFlush);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainInvoke);
		}

		private void ExtractArgumentsAndInvokeEvent(ILGenerator gen, EventInfo eventInfo)
		{
			var method = GenerateInvokeEventMethod(eventInfo);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, method);
		}

		private MethodInfo GenerateInvokeEventMethod(EventInfo eventInfo)
		{
			var methodName = string.Format("Invoke_{0}", eventInfo.Name);
			var method = _typeBuilder.DefineMethod(methodName, MethodAttributes.Private, CallingConventions.HasThis, typeof (void),
			                                       new[]
				                                       {
					                                       typeof (string),
					                                       typeof (BinaryReader),
					                                       typeof (BinaryWriter)
				                                       });

			var gen = method.GetILGenerator();

			var fieldName = EventBackingFieldName(eventInfo.AddMethod.Name);
			var field = _fields[fieldName];
			var delegateType = eventInfo.EventHandlerType;
			var methodInfo = delegateType.GetMethod("Invoke");

			var tmp = gen.DeclareLocal(delegateType);
			var result = gen.DeclareLocal(typeof(bool));
			var dontInvoke = gen.DefineLabel();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, field);
			gen.Emit(OpCodes.Stloc, tmp);
			gen.Emit(OpCodes.Ldloc, tmp);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc, result);
			gen.Emit(OpCodes.Ldloc, result);
			gen.Emit(OpCodes.Brtrue, dontInvoke);

			gen.Emit(OpCodes.Ldloc_0);
			ExtractArgumentsAndCallMethod(gen, methodInfo,
				() => gen.Emit(OpCodes.Ldarg_2),
				() => gen.Emit(OpCodes.Ldarg_3));

			gen.MarkLabel(dontInvoke);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		/// <summary>
		/// Generates the method responsible for invoking the given interface method via
		/// <see cref="IEndPointChannel.CallRemoteMethod"/>.
		/// </summary>
		/// <param name="remoteMethod"></param>
		private void GenerateMethodInvocation(MethodInfo remoteMethod)
		{
			var methodName = remoteMethod.Name;
			var parameters = remoteMethod.GetParameters();
			var method = _typeBuilder.DefineMethod(methodName,
												   MethodAttributes.Public |
													MethodAttributes.Virtual,
													remoteMethod.ReturnType,
													parameters.Select(x => x.ParameterType).ToArray());

			GenerateMethodInvocation(method, methodName, parameters, remoteMethod);

			_typeBuilder.DefineMethodOverride(method, remoteMethod);
		}
	}
}