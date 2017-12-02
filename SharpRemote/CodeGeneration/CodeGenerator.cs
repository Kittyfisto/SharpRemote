using System;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Remoting;

namespace SharpRemote.CodeGeneration
{
	/// <summary>
	///     The default <see cref="ICodeGenerator" /> implementation which generates code on-demand
	///     using IL.Emit.
	/// </summary>
	public sealed class CodeGenerator
		: ICodeGenerator
	{
		private static ICodeGenerator _defaultGenerator;
		private static readonly object DefaultGeneratorConstructionSyncRoot = new object();

		/// <summary>
		///     The default code generator which is shared between all endpoints unless a user supplied
		///     code generator is given.
		/// </summary>
		/// <remarks>
		///     This property is internal because there is no need for a user to have acccess to
		///     this property. Either don't specify code generator, in which case this instance is used,
		///     or create your own code generator if you need your own stuff.
		/// </remarks>
		internal static ICodeGenerator Default
		{
			get
			{
				lock (DefaultGeneratorConstructionSyncRoot)
				{
					return _defaultGenerator ?? (_defaultGenerator = new CodeGenerator());
				}
			}
			
		}

		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="binarySerializer"></param>
		public CodeGenerator(BinarySerializer binarySerializer)
		{
			_proxyCreator = new ProxyCreator(binarySerializer.Module, binarySerializer);
			_servantCreator = new ServantCreator(binarySerializer.Module, binarySerializer);
		}

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <remarks>
		///     Creating a new instance of this type means loading a new dynamic assembly into the
		///     <see cref="AppDomain.CurrentDomain" />. THIS ASSEMBLY CANNOT BE UNLOADED UNLESS THE APPDOMAIN IS.
		///     Do not create new instances of this type if you can easily re-use an existing instance or
		///     you will consume more and more memory.
		/// </remarks>
		/// <param name="customTypeResolver">Type resolver that should be used instead of <see cref="TypeResolver" /></param>
		public CodeGenerator(ITypeResolver customTypeResolver = null)
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleName = assemblyName.Name + ".dll";
			var module = assembly.DefineDynamicModule(moduleName);

			var serializer = new BinarySerializer(module, customTypeResolver);
			_proxyCreator = new ProxyCreator(module, serializer);
			_servantCreator = new ServantCreator(module, serializer);
		}

		/// <inheritdoc />
		public Type GenerateServant<T>()
		{
			return _servantCreator.GenerateServant<T>();
		}

		/// <inheritdoc />
		public IServant CreateServant<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId, T subject)
		{
			return _servantCreator.CreateServant(endPoint, channel, objectId, subject);
		}

		/// <inheritdoc />
		public Type GenerateProxy<T>()
		{
			return _proxyCreator.GenerateProxy<T>();
		}

		/// <inheritdoc />
		public T CreateProxy<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId)
		{
			return _proxyCreator.CreateProxy<T>(endPoint, channel, objectId);
		}
	}
}