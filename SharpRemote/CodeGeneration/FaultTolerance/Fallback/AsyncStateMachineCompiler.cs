#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SharpRemote.CodeGeneration.FaultTolerance.Fallback
{
	/// <summary>
	///     Responsible for creating a state machine capable of intercepting failed tasks and delegating
	///     calls to another new task, in case of failure.
	/// </summary>
	internal sealed class AsyncStateMachineCompiler
	{
		private readonly FieldBuilder _fallback;
		private readonly Type _interfaceType;
		private readonly FieldBuilder _taskCompletionSource;
		private readonly TypeBuilder _stateMachine;
		private readonly Type _taskCompletionSourceType;
		private readonly FieldBuilder _fallbackTask;
		private readonly Type _taskType;
		private readonly Type _taskReturnType;
		private readonly bool _hasReturnValue;
		private readonly MethodInfo _taskCompletionSourceSetResult;
		private readonly List<FieldInfo> _parameters;
		private readonly MethodInfo _originalMethod;
		private readonly MethodInfo _taskCompletionSourceSetException;
		private readonly MethodInfo _taskCompletionSourceSetExceptions;
		private readonly MethodInfo _taskCompletionSourceGetTask;
		private readonly MethodInfo _taskGetAwaiter;
		private readonly Type _awaiterType;
		private readonly MethodInfo _awaiterOnCompleted;
		private readonly MethodInfo _taskGetResult;
		private readonly MethodInfo _taskGetException;
		private readonly FieldBuilder _subject;
		private readonly FieldBuilder _subjectTask;

		public AsyncStateMachineCompiler(TypeBuilder typeBuilder,
		                            ITypeDescription interfaceDescription,
		                            IMethodDescription methodDescription)
		{
			_originalMethod = methodDescription.Method;
			_taskType = methodDescription.ReturnType.Type;
			_taskGetAwaiter = _taskType.GetMethod(nameof(Task.GetAwaiter));
			_taskGetException = _taskType.GetProperty(nameof(Task.Exception)).GetMethod;

			_awaiterType = GetAwaiterType(_taskType);
			_awaiterOnCompleted = _awaiterType.GetMethod(nameof(TaskAwaiter.OnCompleted),
			                                               new[] {typeof(Action)});

			_taskReturnType = GetTaskReturnType(_taskType);
			_hasReturnValue = _taskReturnType != typeof(void);
			if (_hasReturnValue)
			{
				_taskGetResult = _taskType.GetProperty(nameof(Task<int>.Result)).GetMethod;
			}

			_interfaceType = interfaceDescription.Type;
			_taskCompletionSourceType = GetTaskCompletionSourceType(_taskType);
			_taskCompletionSourceSetResult = _taskCompletionSourceType.GetMethod(nameof(TaskCompletionSource<int>.SetResult));
			_taskCompletionSourceSetException = _taskCompletionSourceType.GetMethod(nameof(TaskCompletionSource<int>.SetException),
			                                                                        new []{typeof(Exception)});
			_taskCompletionSourceSetExceptions= _taskCompletionSourceType.GetMethod(nameof(TaskCompletionSource<int>.SetException),
			                                                                        new []{typeof(IEnumerable<Exception>)});
			_taskCompletionSourceGetTask = _taskCompletionSourceType.GetProperty(nameof(TaskCompletionSource<int>.Task))
			                                                        .GetMethod;

			var name = string.Format("{0}_AsyncStateMachine", methodDescription.Name);
			_stateMachine = typeBuilder.DefineNestedType(name);

			_subject = _stateMachine.DefineField("Subject", _taskType, FieldAttributes.Public);
			_fallback = _stateMachine.DefineField("Fallback", _interfaceType, FieldAttributes.Public);

			_subjectTask = _stateMachine.DefineField("_subjectTask", _taskType, FieldAttributes.Private);
			_fallbackTask = _stateMachine.DefineField("_fallbackTask", _taskType, FieldAttributes.Private);
			_taskCompletionSource = _stateMachine.DefineField("TaskCompletionSource", _taskCompletionSourceType,
			                                         FieldAttributes.Public | FieldAttributes.InitOnly);

			_parameters = new List<FieldInfo>(methodDescription.Parameters.Count);
			foreach (var parameter in methodDescription.Parameters)
			{
				var parameterName = string.Format("parameter_{0}", parameter.Name);
				var field = _stateMachine.DefineField(parameterName,
				                                      parameter.ParameterType.Type,
				                                      FieldAttributes.Public);
				_parameters.Add(field);
			}
		}

		public void Compile(ILGenerator targetSite,
		                    FieldInfo subject,
		                    FieldInfo fallback,
		                    LocalBuilder task)
		{
			var failMethodCall = CreateFailMethodCall();
			var onFallbackCompleted = CreateOnFallbackCompleted(failMethodCall);
			var invokeFallback = CreateInvokeFallback(onFallbackCompleted, failMethodCall);
			var onSubjectCompleted = CreateOnSubjectCompleted(invokeFallback);
			var constructor = CreateConstructor();
			var start = CreateInvokeSubject(onSubjectCompleted);
			_stateMachine.CreateType();

			StartStateMachineAtTargetSite(targetSite,
			                              subject,
			                              fallback,
			                              constructor,
			                              start,
			                              task);
		}

		private MethodInfo CreateInvokeSubject(MethodInfo onSubjectCompleted)
		{
			var method = _stateMachine.DefineMethod("InvokeSubject",
			                                        MethodAttributes.Public,
			                                        typeof(void),
			                                        new Type[0]);

			var gen = method.GetILGenerator();

#if TRACE
			gen.EmitWriteLine("InvokeSubject Start");
#endif

			// _subjectTask = _subject.Do(....)
			gen.Emit(OpCodes.Ldarg_0);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);
			for (int i = 0; i < _parameters.Count; ++i)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _parameters[i]);
			}

			gen.Emit(OpCodes.Callvirt, _originalMethod);
			gen.Emit(OpCodes.Stfld, _subjectTask);

			// var awaiter = _subjectTask.GetAwaiter();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subjectTask);
			var awaiter = gen.DeclareLocal(_awaiterType);
			gen.Emit(OpCodes.Callvirt, _taskGetAwaiter);
			gen.Emit(OpCodes.Stloc, awaiter);
			
			// awaiter.ContinueWith(OnSubjectCompleted)
			var actionCtor = typeof(Action).GetConstructor(new[] {typeof(object), typeof(IntPtr)});
			gen.Emit(OpCodes.Ldloca, awaiter);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, onSubjectCompleted);
			gen.Emit(OpCodes.Newobj, actionCtor);
			gen.Emit(OpCodes.Call, _awaiterOnCompleted);

#if TRACE
			gen.EmitWriteLine("InvokeSubject End");
#endif

			gen.Emit(OpCodes.Ret);

			return method;
		}

		/// <summary>
		///     Injects code necessary to start the async state machine at the target site.
		///     Moves the reference to the state machine's task into the given local <paramref name="task" />.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="subject"></param>
		/// <param name="fallback"></param>
		/// <param name="constructor"></param>
		/// <param name="invokeSubject"></param>
		/// <param name="task"></param>
		private void StartStateMachineAtTargetSite(ILGenerator gen,
		                                           FieldInfo subject,
		                                           FieldInfo fallback,
		                                           ConstructorInfo constructor,
		                                           MethodInfo invokeSubject,
		                                           LocalBuilder task)
		{
			var asyncStateMachine = gen.DeclareLocal(_stateMachine);

			// var asyncStateMachine = new AsyncStateMachine();
			gen.Emit(OpCodes.Newobj, constructor);
			gen.Emit(OpCodes.Stloc, asyncStateMachine);

			// asyncStateMachine.Subject = this._subject
			gen.Emit(OpCodes.Ldloc, asyncStateMachine);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, subject);
			gen.Emit(OpCodes.Stfld, _subject);

			// asyncStateMachine.Fallback = this._fallback
			gen.Emit(OpCodes.Ldloc, asyncStateMachine);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, fallback);
			gen.Emit(OpCodes.Stfld, _fallback);

			// asyncStateMachine._argumentN = argumentN
			for (int i = 0; i < _parameters.Count; ++i)
			{
				gen.Emit(OpCodes.Ldloc, asyncStateMachine);
				gen.Emit(OpCodes.Ldarg, i + 1);
				gen.Emit(OpCodes.Stfld, _parameters[i]);
			}

			gen.Emit(OpCodes.Ldloc, asyncStateMachine);
			gen.Emit(OpCodes.Call, invokeSubject);

			// Finally we move the state machine's task to the local variable
			// we were given (so it can be consumed by the caller).
			gen.Emit(OpCodes.Ldloc, asyncStateMachine);
			gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
			gen.Emit(OpCodes.Callvirt, _taskCompletionSourceGetTask);
			gen.Emit(OpCodes.Stloc, task);
		}

		private MethodInfo CreateFailMethodCall()
		{
			var method = _stateMachine.DefineMethod("FailMethodCall",
			                                        MethodAttributes.Private,
			                                        CallingConventions.Standard | CallingConventions.HasThis,
			                                        typeof(void),
			                                        new[] {typeof(Exception)});

			var gen = method.GetILGenerator();

			var aggregateException = gen.DeclareLocal(typeof(AggregateException));
			var innerExceptions = gen.DeclareLocal(typeof(Exception[]));

#if TRACE
			gen.EmitWriteLine("FailMethodCall Start");
#endif

			var end = gen.DefineLabel();
			var notAggregateException = gen.DefineLabel();

			// var aggregateException = e as AggregateException
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Isinst, typeof(AggregateException));
			gen.Emit(OpCodes.Stloc, aggregateException);

			// if (aggregateException != null)
			gen.Emit(OpCodes.Ldloc, aggregateException);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Cgt_Un);
			gen.Emit(OpCodes.Brfalse_S, notAggregateException);

			// var exceptions = aggregateException.InnerExceptions;
			var aggregateExceptionGetInnerExceptions = typeof(AggregateException)
			                                           .GetProperty(nameof(AggregateException.InnerExceptions))
			                                           .GetMethod;
			gen.Emit(OpCodes.Ldloc, aggregateException);
			gen.Emit(OpCodes.Callvirt, aggregateExceptionGetInnerExceptions);
			gen.Emit(OpCodes.Stloc, innerExceptions);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
			gen.Emit(OpCodes.Ldloc, innerExceptions);
			gen.Emit(OpCodes.Callvirt, _taskCompletionSourceSetExceptions);
			gen.Emit(OpCodes.Br_S, end);

			gen.MarkLabel(notAggregateException);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, _taskCompletionSourceSetException);

			gen.MarkLabel(end);

#if TRACE
			gen.EmitWriteLine("FailMethodCall End");
#endif

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateOnFallbackCompleted(MethodInfo failMethodCall)
		{
			var method = _stateMachine.DefineMethod("OnFallbackCompleted",
			                                        MethodAttributes.Private,
			                                        typeof(void),
			                                        new Type[0]);

			var gen = method.GetILGenerator();
			var exception = gen.DeclareLocal(typeof(Exception));
			var end = gen.DefineLabel();
			var noException = gen.DefineLabel();

#if TRACE
			gen.EmitWriteLine("OnFallbackCompleted Start");
#endif

			// try {...
			gen.BeginExceptionBlock();

			// var exception = _fallbackTask.Exception
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _fallbackTask);
			gen.Emit(OpCodes.Callvirt, _taskGetException);
			gen.Emit(OpCodes.Stloc, exception);

			// if (exception != null)
			gen.Emit(OpCodes.Ldloc, exception);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Cgt_Un);
			gen.Emit(OpCodes.Brfalse_S, noException);

			// FailMethodCall(exception)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc, exception);
			gen.Emit(OpCodes.Call, failMethodCall);
			gen.Emit(OpCodes.Br_S, end);
			
			gen.MarkLabel(noException);
			if (_hasReturnValue)
			{
				// _taskCompletionSource.SetResult(_fallbackTask.Result);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _fallbackTask);
				gen.Emit(OpCodes.Callvirt, _taskGetResult);
				gen.Emit(OpCodes.Call, _taskCompletionSourceSetResult);
			}
			else
			{
				// _taskCompletionSource.SetResult(42);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
				gen.Emit(OpCodes.Ldc_I4, 42);
				gen.Emit(OpCodes.Call, _taskCompletionSourceSetResult);
			}

			// cach(Exception e) { FailMethodCall(e); }
			gen.BeginCatchBlock(typeof(Exception));
			gen.Emit(OpCodes.Stloc, exception);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc, exception);
			gen.Emit(OpCodes.Call, failMethodCall);


			gen.EndExceptionBlock();
			gen.MarkLabel(end);

#if TRACE
			gen.EmitWriteLine("OnFallbackCompleted End");
#endif

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateInvokeFallback(MethodInfo onFallbackCompleted, MethodInfo failMethodCall)
		{
			var method = _stateMachine.DefineMethod("InvokeFallback",
			                                        MethodAttributes.Private,
			                                        typeof(void),
			                                        new Type[0]);

			var gen = method.GetILGenerator();
			var exception = gen.DeclareLocal(typeof(Exception));

#if TRACE
			gen.EmitWriteLine("InvokeFallback Start");
#endif

			// try { ...
			gen.BeginExceptionBlock();

			// _fallbackTask = _fallback.Do(....)
			gen.Emit(OpCodes.Ldarg_0);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _fallback);

			for (int i = 0; i < _parameters.Count; ++i)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _parameters[i]);
			}

			gen.Emit(OpCodes.Callvirt, _originalMethod);
			gen.Emit(OpCodes.Stfld, _fallbackTask);

			// var awaiter = _fallbackTask.GetAwaiter();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _fallbackTask);
			var awaiter = gen.DeclareLocal(_awaiterType);
			gen.Emit(OpCodes.Callvirt, _taskGetAwaiter);
			gen.Emit(OpCodes.Stloc, awaiter);
			
			// awaiter.ContinueWith(OnFallbackCompleted)
			var actionCtor = typeof(Action).GetConstructor(new[] {typeof(object), typeof(IntPtr)});
			gen.Emit(OpCodes.Ldloca, awaiter);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, onFallbackCompleted);
			gen.Emit(OpCodes.Newobj, actionCtor);
			gen.Emit(OpCodes.Call, _awaiterOnCompleted);

			// cach(Exception e) { FailMethodCall(e); }
			gen.BeginCatchBlock(typeof(Exception));
			gen.Emit(OpCodes.Stloc, exception);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc, exception);
			gen.Emit(OpCodes.Call, failMethodCall);

			gen.EndExceptionBlock();

#if TRACE
			gen.EmitWriteLine("InvokeFallback End");
#endif

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private MethodInfo CreateOnSubjectCompleted(MethodInfo invokeFallback)
		{
			var method = _stateMachine.DefineMethod("OnSubjectCompleted",
			                                        MethodAttributes.Private,
			                                        typeof(void),
			                                        new Type[0]);

			var gen = method.GetILGenerator();
			var end = gen.DefineLabel();
			var exception = gen.DeclareLocal(typeof(Exception));
			var noException = gen.DefineLabel();

#if TRACE
			gen.EmitWriteLine("OnSubjectCompleted Start");
#endif

			// try { ... }
			gen.BeginExceptionBlock();

			// var exception = _fallbackTask.Exception
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subjectTask);
			gen.Emit(OpCodes.Callvirt, _taskGetException);

			// if (exception != null)
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Cgt_Un);
			gen.Emit(OpCodes.Brfalse_S, noException);

			// InvokeFallback()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, invokeFallback);
			gen.Emit(OpCodes.Br_S, end);

			gen.MarkLabel(noException);
			if (_hasReturnValue)
			{
				// _taskCompletionSource.SetResult(_originalTask.Result);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _taskCompletionSource);

				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _subjectTask);
				gen.Emit(OpCodes.Callvirt, _taskGetResult);

				gen.Emit(OpCodes.Callvirt, _taskCompletionSourceSetResult);
			}
			else
			{
				// _taskCompletionSource.SetResult(42)
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, _taskCompletionSource);
				gen.Emit(OpCodes.Ldc_I4, 42);
				gen.Emit(OpCodes.Callvirt, _taskCompletionSourceSetResult);
			}

			gen.BeginCatchBlock(typeof(Exception));
			// catch(Exception) { InvokeFallback(); }
			gen.Emit(OpCodes.Stloc, exception);
			gen.EmitWriteLine(exception);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, invokeFallback);

			gen.EndExceptionBlock();

			gen.MarkLabel(end);

#if TRACE
			gen.EmitWriteLine("OnSubjectCompleted End");
#endif

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private ConstructorInfo CreateConstructor()
		{
			var constructor = _stateMachine.DefineConstructor(MethodAttributes.Public,
			                                                  CallingConventions.Standard | CallingConventions.HasThis,
			                                                  new Type[0]);

			var gen = constructor.GetILGenerator();

#if TRACE
			gen.EmitWriteLine("Constructor Start");
#endif

			// _taskCompletionSource = new TaskCompletionSource<>()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Newobj, _taskCompletionSourceType.GetConstructor(new Type[0]));
			gen.Emit(OpCodes.Stfld, _taskCompletionSource);

#if TRACE
			gen.EmitWriteLine("Constructor End");
#endif

			gen.Emit(OpCodes.Ret);

			return constructor;
		}

		private static Type GetAwaiterType(Type taskType)
		{
			if (taskType.IsGenericType) return typeof(TaskAwaiter<>).MakeGenericType(taskType.GetGenericArguments()[0]);

			return typeof(TaskAwaiter);
		}

		private static Type GetTaskCompletionSourceType(Type taskType)
		{
			if (taskType.IsGenericType) return typeof(TaskCompletionSource<>).MakeGenericType(taskType.GetGenericArguments()[0]);

			return typeof(TaskCompletionSource<int>);
		}

		[Pure]
		private static Type GetTaskReturnType(Type taskType)
		{
			if (taskType.IsGenericType)
				return taskType.GetGenericArguments()[0];

			return typeof(void);
		}
	}
}