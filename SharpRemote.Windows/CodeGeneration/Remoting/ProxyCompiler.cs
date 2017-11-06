using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using SharpRemote.Tasks;

namespace SharpRemote.CodeGeneration.Remoting
{
	internal sealed class ProxyCompiler
		: Compiler
	{
		private readonly ModuleBuilder _module;
		private readonly TypeBuilder _typeBuilder;
		private readonly Dictionary<string, FieldBuilder> _fields;
		private readonly Dictionary<EventInfo, FieldBuilder> _perEventSchedulers;
		private FieldBuilder _perTypeScheduler;
		private FieldBuilder _perObjectScheduler;

		public ProxyCompiler(BinarySerializer binarySerializer, ModuleBuilder module, string proxyTypeName, Type interfaceType)
			: base(binarySerializer, interfaceType)
		{
			if (module == null) throw new ArgumentNullException(nameof(module));
			if (proxyTypeName == null) throw new ArgumentNullException(nameof(proxyTypeName));

			_module = module;

			_typeBuilder = _module.DefineType(proxyTypeName, TypeAttributes.Class, typeof (object), new[]
				{
					interfaceType
				});
			_typeBuilder.AddInterfaceImplementation(typeof(IProxy));
			_perEventSchedulers = new Dictionary<EventInfo, FieldBuilder>();

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
			GenerateCctor();
			GenerateCtor();
			GenerateGetObjectId();
			GenerateGetSerializer();
			GenerateGetEndPoint();
			GenerateMethods();
			GenerateInvokeEvent();
			GenerateGetTaskScheduler();
			GenerateInterfaceType();

			var proxyType = _typeBuilder.CreateType();
			return proxyType;
		}

		private void GenerateInterfaceType()
		{
			var getInterfaceType = _typeBuilder.DefineMethod("get_InterfaceType",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
				, typeof(Type), null);
			var gen = getInterfaceType.GetILGenerator();
			gen.Emit(OpCodes.Ldtoken, InterfaceType);
			gen.Emit(OpCodes.Call, Methods.TypeGetTypeFromHandle);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(getInterfaceType, Methods.GrainGetInterfaceType);
		}

		private void GenerateCctor()
		{
			var allEvents = AllEvents
				.Where(x => x.GetCustomAttribute<InvokeAttribute>() != null &&
				            x.GetCustomAttribute<InvokeAttribute>().DispatchingStrategy == Dispatch.SerializePerType)
				.ToList();

			if (allEvents.Count == 0)
				return;

			_perTypeScheduler = _typeBuilder.DefineField(
				"PerTypeScheduler",
				typeof (SerialTaskScheduler),
				FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.Static);

			var cctor = _typeBuilder.DefineTypeInitializer();
			var gen = cctor.GetILGenerator();

			gen.Emit(OpCodes.Ldstr, InterfaceType.FullName);
			gen.Emit(OpCodes.Ldnull);

			var loc = gen.DeclareLocal(typeof(ulong?));
			gen.Emit(OpCodes.Ldloca, loc);
			gen.Emit(OpCodes.Initobj, typeof(ulong?));
			gen.Emit(OpCodes.Ldloc, loc);

			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Newobj, Methods.SerialTaskSchedulerCtor);
			gen.Emit(OpCodes.Stsfld, _perTypeScheduler);
			gen.Emit(OpCodes.Ret);
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

			var perObjectEvents = AllEvents
				.Where(x => x.GetCustomAttribute<InvokeAttribute>() != null &&
				            x.GetCustomAttribute<InvokeAttribute>().DispatchingStrategy == Dispatch.SerializePerObject)
				.ToList();

			if (perObjectEvents.Count > 0)
			{
				_perObjectScheduler = _typeBuilder.DefineField("_perObjectScheduler",
				                                               typeof (SerialTaskScheduler),
				                                               FieldAttributes.Private | FieldAttributes.InitOnly);

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldstr, InterfaceType.FullName);
				gen.Emit(OpCodes.Ldnull);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Newobj, Methods.NullableUInt64Ctor);
				gen.Emit(OpCodes.Ldc_I4_0);
				gen.Emit(OpCodes.Newobj, Methods.SerialTaskSchedulerCtor);
				gen.Emit(OpCodes.Stfld, _perObjectScheduler);
			}

			var perMethodEvents = AllEvents
				.Where(x => x.GetCustomAttribute<InvokeAttribute>() != null &&
				            x.GetCustomAttribute<InvokeAttribute>().DispatchingStrategy == Dispatch.SerializePerMethod)
				.ToList();

			foreach (var eventInfo in perMethodEvents)
			{
				var scheduler = _typeBuilder.DefineField(string.Format("_{0}", eventInfo.Name),
															   typeof(SerialTaskScheduler),
															   FieldAttributes.Private | FieldAttributes.InitOnly);

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldstr, InterfaceType.FullName);
				gen.Emit(OpCodes.Ldstr, eventInfo.Name);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Newobj, Methods.NullableUInt64Ctor);
				gen.Emit(OpCodes.Ldc_I4_0);
				gen.Emit(OpCodes.Newobj, Methods.SerialTaskSchedulerCtor);
				gen.Emit(OpCodes.Stfld, scheduler);

				_perEventSchedulers.Add(eventInfo, scheduler);
			}

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

		private void GenerateGetEndPoint()
		{
			var method = _typeBuilder.DefineMethod("get_EndPoint", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof(IRemotingEndPoint), null);
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, EndPoint);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetEndPoint);
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

		private EventInfo[] AllEvents
		{
			get { return InterfaceType.GetEvents(); }
		}

		private void GenerateMethods()
		{
			var methodNames = new HashSet<string>();
			var allMethods = new List<MethodInfo>();

			foreach (MethodInfo method in InterfaceType
				.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
				.Concat(
					InterfaceType.GetInterfaces()
					             .SelectMany(
						             x => x.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)))
				.OrderBy(x => x.Name))
			{
				if (!methodNames.Add(method.Name))
				{
					throw new ArgumentException(
						string.Format("The type contains at least two methods with the same name '{0}': This is not supported",
						              method.Name));
				}

				allMethods.Add(method);
			}

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

		private void GenerateGetTaskScheduler()
		{
			MethodBuilder method = _typeBuilder.DefineMethod("GetTaskScheduler",
															 MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
															 typeof(SerialTaskScheduler),
															 new[] { typeof(string) });

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder name = gen.DeclareLocal(typeof(string));
			Label @throw = gen.DefineLabel();
			Label @ret = gen.DefineLabel();

			// if (method == null) goto ret
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stloc, name);
			gen.Emit(OpCodes.Ldloc, name);
			gen.Emit(OpCodes.Brfalse, @throw);

			var allEvents = AllEvents;

			var labels = new Label[allEvents.Length];
			int index = 0;
			foreach (EventInfo eventInfo in allEvents)
			{
				gen.Emit(OpCodes.Ldloc, name);
				gen.Emit(OpCodes.Ldstr, eventInfo.Name);
				gen.Emit(OpCodes.Call, Methods.StringEquality);

				Label @true = gen.DefineLabel();
				labels[index++] = @true;
				gen.Emit(OpCodes.Brtrue, @true);
			}

			gen.Emit(OpCodes.Br, @throw);

			for (int i = 0; i < allEvents.Length; ++i)
			{
				EventInfo eventInfo = allEvents[i];
				Label label = labels[i];

				gen.MarkLabel(label);
				var attribute = eventInfo.GetCustomAttribute<InvokeAttribute>();
				var strategy = attribute != null ? attribute.DispatchingStrategy : Dispatch.DoNotSerialize;

				switch (strategy)
				{
					case Dispatch.DoNotSerialize:
						gen.Emit(OpCodes.Ldnull);
						break;

					case Dispatch.SerializePerMethod:
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldfld, _perEventSchedulers[eventInfo]);
						break;

					case Dispatch.SerializePerObject:
						gen.Emit(OpCodes.Ldarg_0);
						gen.Emit(OpCodes.Ldfld, _perObjectScheduler);
						break;

					case Dispatch.SerializePerType:
						gen.Emit(OpCodes.Ldsfld, _perTypeScheduler);
						break;

					default:
						throw new InvalidEnumArgumentException("InvokeAttribute.DispatchingStrategy", (int)strategy, typeof(Dispatch));
				}

				gen.Emit(OpCodes.Br, @ret);
			}

			gen.MarkLabel(@throw);
			gen.Emit(OpCodes.Ldstr, "Event '{0}' not found");
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, Methods.StringFormatOneObject);
			gen.Emit(OpCodes.Newobj, Methods.ArgumentExceptionCtor);
			gen.Emit(OpCodes.Throw);

			gen.MarkLabel(@ret);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetTaskScheduler);
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

			var allEvents = AllEvents;
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
			gen.Emit(OpCodes.Call, Methods.StringFormatOneObject);
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

			GenerateMethodInvocation(method, InterfaceType.FullName, methodName, parameters, remoteMethod);

			_typeBuilder.DefineMethodOverride(method, remoteMethod);
		}
	}
}