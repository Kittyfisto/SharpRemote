using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRemote.WebApi.Requests
{
	internal sealed class RequestHandlerCompiler
		: IRequestHandlerCreator
	{
		private readonly ModuleBuilder _module;
		private readonly Dictionary<Type, Type> _requestHandlers;
		private readonly object _syncRoot;

		public RequestHandlerCompiler(ModuleBuilder module)
		{
			if (module == null)
				throw new ArgumentNullException(nameof(module));

			_module = module;
			_syncRoot = new object();
			_requestHandlers = new Dictionary<Type, Type>();
		}

		public IRequestHandler Create<T>(T controller)
		{
			var type = typeof(T);
			lock (_syncRoot)
			{
				Type handlerType;
				if (!_requestHandlers.TryGetValue(type, out handlerType))
				{
					handlerType = CompileRequestHandler(type);
					_requestHandlers.Add(type, handlerType);
				}
				var handler = Activator.CreateInstance(handlerType, controller);
				return (IRequestHandler) handler;
			}
		}

		private Type CompileRequestHandler(Type controllerType)
		{
			var name = string.Format("{0}RequestHandler", controllerType.Name);
			var fullName = string.Format("SharpRemote.WebApi.{0}", name);
			var type = _module.DefineType(fullName);
			var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
			
			return type;
		}
	}
}