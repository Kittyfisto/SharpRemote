using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using log4net;

namespace SharpRemote.CodeGeneration
{
	/// <summary>
	///     Represents a .NET type that may or may not be loaded into the current AppDomain
	///     (as opposed to <see cref="Type" /> which can ONLY represent types loaded into the current
	///     AppDomain).
	/// </summary>
	internal sealed class TypeName
	{
		#region Fields

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		///     Cache to store assembly names for types which were already found
		/// </summary>
		private static readonly Dictionary<string, AssemblyName> TypeDictionary = new Dictionary<string, AssemblyName>();

		private readonly TypeName _genericTypeDefinition;
		private readonly AssemblyName _assemblyName;
		private readonly string _name;
		private readonly string _namespace;
		private readonly string _fullTypeNameWithNamespace;
		private readonly TypeName[] _genericTypeArguments;
		private readonly TypeName _declaringType;

		#endregion

		#region Properties

		/// <summary>
		///     The type-name of this type, ignoring any inner types (only differs from
		///     <see cref="_fullTypeNameWithNamespace" /> in case this is a generic type.
		/// </summary>
		public TypeName GenericTypeDefinition => _genericTypeDefinition;

		public bool IsGenericTypeDefinition => GenericTypeArguments != null && GenericTypeArguments.Any(x => x == null);

		/// <summary>
		///     The generic-type arguments, if there are any.
		///     Is set null for each argument that is not specified,
		///     for example List{} has one generic argument that is not specified, hence
		///     the reference will be null.
		/// </summary>
		public TypeName[] GenericTypeArguments => _genericTypeArguments;

		public TypeName DeclaringType => _declaringType;

		/// <summary>
		///     The amount of generic arguments this type contains.
		/// </summary>
		public int NumGenericArguments => GenericTypeArguments != null ? GenericTypeArguments.Length : 0;

		public bool IsGenericType => NumGenericArguments > 0;

		/// <summary>
		///     The full typename (e.g. System.List{System.Int}).
		/// </summary>
		public string FullName
		{
			get
			{
				if (_assemblyName == null)
					return _fullTypeNameWithNamespace;

				return string.Format("{0}, {1}", _fullTypeNameWithNamespace, _assemblyName);
			}
		}

		public string Name => _name;

		public string Namespace => _namespace;

		/// <summary>
		///     The full typename (e.g. System.List{System.Int}).
		/// </summary>
		public string FullTypeNameWithNamespace => _fullTypeNameWithNamespace;

		public AssemblyName AssemblyName => _assemblyName;

		public bool IsArray => Name.EndsWith("[]");

		#endregion

		#region Constructor

		private TypeName(string typeNameWithNamespace,
			AssemblyName assemblyName,
			TypeName declaringType,
			TypeName genericTypeDefinition,
			params TypeName[] genericTypeArguments)
		{
			if (string.IsNullOrWhiteSpace(typeNameWithNamespace))
				throw new ArgumentOutOfRangeException(nameof(typeNameWithNamespace));

			_fullTypeNameWithNamespace = typeNameWithNamespace;
			_genericTypeDefinition = genericTypeDefinition;
			_assemblyName = assemblyName;
			_genericTypeArguments = genericTypeArguments;
			_declaringType = declaringType;

			// ReSharper disable once StringIndexOfIsCultureSpecific.1
			var indexOfGenericType = typeNameWithNamespace.IndexOf("[[");
			if (IsGenericType && indexOfGenericType != -1)
			{
				var typeNameWithNamespaceWithoutGenericType = typeNameWithNamespace.Substring(0, indexOfGenericType);
				GetName(typeNameWithNamespaceWithoutGenericType, out _name, out _namespace);
			}
			else
			{
				GetName(typeNameWithNamespace, out _name, out _namespace);
			}
		}

		#endregion

		#region Public Methods

		public static AssemblyName FindAssembly(string typeName)
		{
			lock (TypeDictionary)
			{
				if (TypeDictionary.ContainsKey(typeName))
					return TypeDictionary[typeName];
			}

			foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach (var t in ass.GetTypes())
					{
						if (string.Equals(t.FullName, typeName, StringComparison.InvariantCulture))
						{
							lock (TypeDictionary)
							{
								// We do NOT lock the entire method, therefore
								// we cannot assume that the given type hasn't been added before.
								TypeDictionary[typeName] = ass.GetName();
							}

							return ass.GetName();
						}
					}
				}
				catch (ReflectionTypeLoadException ex)
				{
					var buildString = string.Join(", ", ex.LoaderExceptions.Where(x => x != null).Select(x => x.ToString()));
					Log.ErrorFormat("exception {0} while searching for type {1}: {2}", ex.Message, typeName, buildString);
				}
			}

			return null;
		}

		public static bool TryParse(string typeName, out TypeName ret)
		{
			// It's possible that we're not given a fully quallified name
			var split = SplitStrongName(typeName);
			if (split.Length != 1 && split.Length != 2)
			{
				ret = null;
				return false;
			}

			var assembly = split.Length >= 2 ? new AssemblyName(split[1]) : null;

			ret = Create(split[0], assembly);
			return true;
		}


		public static TypeName Parse(string typeName)
		{
			if (!TryParse(typeName, out var ret))
				throw new FormatException(string.Format("'{0}' does not contain a representation of a .NET type-name", typeName));

			return ret;
		}

		/// <summary>
		///     Tries to create a new <see cref="TypeName" /> object that represents the given full typename.
		///     The object contains a description of all generic type arguments, if the typename contains any.
		/// </summary>
		/// <param name="fullTypeName"></param>
		/// <param name="assemblyName"></param>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public static bool TryCreate(string fullTypeName, AssemblyName assemblyName, out TypeName typeName)
		{
			if (!TryParseGenericTypeArguments(fullTypeName, out var genericTypeDefinitionName, out var genericTypeArguments))
			{
				typeName = null;
				return false;
			}

			// In fullTypeName refers to a closed generic type, then we will never find an assembly
			// that contains that type (for example there is no assembly that contains the type
			// "System.Collections.Generic.List`1[[System.Int]]", but there most certainly is one that
			// contains "System.Collections.Generic.List`1". Thus the assembly-lookup needs to be performed
			// on the generic-type-definition, not the fullTypeName.
			if (assemblyName == null)
			{
				assemblyName = FindAssembly(genericTypeDefinitionName ?? fullTypeName);
			}

			TypeName genericTypeDefinition = null;
			if (genericTypeDefinitionName != null)
			{
				var args = new TypeName[genericTypeArguments.Length];
				genericTypeDefinition = new TypeName(genericTypeDefinitionName, assemblyName, null, null, args);
			}

			TypeName declaringType = null;
			if (TryParseDeclaringType(fullTypeName, out var declaringTypeName, out _))
			{
				declaringType = new TypeName(declaringTypeName, assemblyName, null, null);
			}

			typeName = new TypeName(fullTypeName, assemblyName, declaringType, genericTypeDefinition, genericTypeArguments);
			return true;
		}

		[Pure]
		public static TypeName Create(string typeNameWithNamespace, AssemblyName assemblyName)
		{
			if (!TryCreate(typeNameWithNamespace, assemblyName, out var typeName))
				throw new FormatException(string.Format("'{0}' does not contain a representation of a .NET type-name", typeName));

			return typeName;
		}

		[Pure]
		public static TypeName Create(string typeNameWithNamespace, string assemblyName = null)
		{
			return Create(typeNameWithNamespace, assemblyName != null ? new AssemblyName(assemblyName) : null);
		}

		/// <summary>
		///     Creates a generic type from this generic type definition and the given arguments.
		/// </summary>
		/// <param name="genericTypeArguments"></param>
		/// <returns></returns>
		public TypeName MakeGenericType(params TypeName[] genericTypeArguments)
		{
			Contract.Requires<ArgumentException>(IsGenericTypeDefinition);

			var fullName = BuildFullName(this, genericTypeArguments);
			return new TypeName(fullName,
				_assemblyName,
				null,
				this,
				genericTypeArguments);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is TypeName))
				return false;

			return Equals((TypeName)obj);
		}

		public override int GetHashCode()
		{
			return FullName.GetHashCode();
		}

		public override string ToString()
		{
			return FullName;
		}

		#endregion

		#region Protected Methods

		/// <summary>
		///     Extracts the number of generic type arguments and returns the index of the
		///     start of the first generic type argument in the given full typename.
		/// </summary>
		/// <param name="fullTypename"></param>
		/// <param name="genericTypeDefinition"></param>
		/// <param name="indexOfFirstArgument"></param>
		/// <param name="numberOfGenericTypeArguments"></param>
		/// <returns></returns>
		internal static bool TryParseNumberOfGenericTypeArguments(string fullTypename,
			out string genericTypeDefinition,
			out int indexOfFirstArgument,
			out int numberOfGenericTypeArguments)
		{
			var pos = fullTypename.IndexOf('`');
			if (pos == -1) //< no '? => it's no generic type then
			{
				indexOfFirstArgument = -1;
				numberOfGenericTypeArguments = 0;
				genericTypeDefinition = null;
				return true;
			}

			pos += 1;
			var end = fullTypename.IndexOf('[', pos);
			if (end == -1)
			{
				// "System.List`1" is a valid typename, as is
				// "System.List`1[[System.Int]]" => if we cannot find a [ then
				// we assume that fullTypename is like the first example....
				end = fullTypename.Length;
			}

			var num = fullTypename.Substring(pos, end - pos);
			if (!int.TryParse(num, out numberOfGenericTypeArguments) || numberOfGenericTypeArguments < 1)
			{
				indexOfFirstArgument = -1;
				numberOfGenericTypeArguments = 0;
				genericTypeDefinition = null;
				return false;
			}

			genericTypeDefinition = fullTypename.Substring(0, end);
			indexOfFirstArgument = end + 1;
			return true;
		}

		/// <summary>
		///     Extracts the typename of the next generic type argument from the given string.
		///     Expects the type argument to start at the given position, e.g. the first character
		///     must be '[', otherwise the full typename is assumed to be malformatted.
		/// </summary>
		/// <param name="fullTypename"></param>
		/// <param name="start"></param>
		/// <param name="fullyQualifiedTypename"></param>
		/// <returns></returns>
		internal static bool TryExtractGenericTypeArgument(string fullTypename,
			ref int start,
			out string fullyQualifiedTypename)
		{
			var bcount = 0;
			for (var i = start; i < fullTypename.Length; ++i)
			{
				var c = fullTypename[i];
				if (c == '[')
				{
					++bcount;
				}
				else if (c == ']')
				{
					--bcount;

					if (bcount == 0)
					{
						fullyQualifiedTypename = fullTypename.Substring(start + 1, i - start - 1);
						start = i + 1;
						return true;
					}
				}
			}

			fullyQualifiedTypename = null;
			return false;
		}

		#endregion

		#region Private Methods

		private static void GetName(string typeNameWithNamespace, out string name, out string @namespace)
		{
			var index = typeNameWithNamespace.LastIndexOf('.');
			if (index != -1)
			{
				@namespace = typeNameWithNamespace.Substring(0, index);
				name = typeNameWithNamespace.Substring(index + 1);
			}
			else
			{
				@namespace = string.Empty;
				name = typeNameWithNamespace;
			}

			index = name.LastIndexOf("+");
			if (index != -1)
				name = name.Substring(index + 1);
		}

		/// <summary>
		///     Splits the strong name into two parts: One identifying the type itself (namespace.typename)
		///     and the other part identifying the assembly (assembly name, version, culture, publickey).
		/// </summary>
		/// <param name="strongName"></param>
		/// <returns></returns>
		private static string[] SplitStrongName(string strongName)
		{
			var index = -1;
			var bcount = 0;
			for (var i = 0; i < strongName.Length; ++i)
			{
				if (strongName[i] == '[')
					++bcount;
				else if (strongName[i] == ']')
					--bcount;
				else if (bcount == 0 && strongName[i] == ',')
				{
					index = i;
					break;
				}
			}

			if (index == -1)
				return new[] { strongName };

			var typeName = strongName.Substring(0, index);
			var what = strongName.Substring(index + 1);
			return new[] { typeName, what };
		}

		private bool Equals(TypeName that)
		{
			return string.Equals(FullName, that.FullName);
		}

		private static bool TryParseDeclaringType(string fullTypename,
			out string declaringTypename,
			out string innerTypename)
		{
			var index = fullTypename.IndexOf('+');
			if (index == -1)
			{
				declaringTypename = null;
				innerTypename = null;
				return false;
			}

			declaringTypename = fullTypename.Substring(0, index);
			innerTypename = fullTypename.Substring(index + 1);
			return true;
		}

		private static bool TryParseGenericTypeArguments(string fullTypename,
			out string genericTypeDefinition,
			out TypeName[] genericTypeArguments)
		{
			//
			// Is it an array type such as Foo[]?
			//

			if (fullTypename.EndsWith("[]"))
			{
				var arrayTypeName = fullTypename.Substring(0, fullTypename.Length - 2);
				if (!TryParse(arrayTypeName, out var arrayType))
				{
					genericTypeDefinition = null;
					genericTypeArguments = null;
					return false;
				}

				genericTypeDefinition = "System.Array";
				genericTypeArguments = new[] { arrayType };
				return true;
			}


			//
			// Find out how many arguments this type has, like in "blub`1[["
			//

			if (!TryParseNumberOfGenericTypeArguments(fullTypename,
				    out genericTypeDefinition,
				    out var firstArgumentIndex,
				    out var numTypeArguments))
			{
				genericTypeArguments = null;
				return false;
			}

			genericTypeArguments = new TypeName[numTypeArguments];
			if (firstArgumentIndex <= fullTypename.Length)
			{
				//
				// Parse each argument's typename as well..
				//

				for (var i = 0; i < numTypeArguments; ++i)
				{
					if (!TryExtractGenericTypeArgument(fullTypename, ref firstArgumentIndex, out var fullyQualifiedTypename))
					{
						genericTypeArguments = null;
						return false;
					}

					if (!string.IsNullOrWhiteSpace(fullyQualifiedTypename))
					{
						if (!TryParse(fullyQualifiedTypename, out var genericTypename))
						{
							genericTypeArguments = null;
							return false;
						}

						genericTypeArguments[i] = genericTypename;
					}

					var c = fullTypename[firstArgumentIndex];
					if (c == ',')
						++firstArgumentIndex;
				}
			}

			return true;
		}

		private static string BuildFullName(TypeName genericTypeDefinition, IEnumerable<TypeName> genericTypeArguments)
		{
			var args = string.Join(", ", genericTypeArguments.Select(x => string.Format("[{0}]", x.FullName)));

			if (genericTypeDefinition._assemblyName == null)
				return string.Format("{0}[{1}]", genericTypeDefinition._fullTypeNameWithNamespace, args);

			return string.Format("{0}[{1}], {2}",
				genericTypeDefinition._fullTypeNameWithNamespace,
				args,
				genericTypeDefinition._assemblyName);
		}

		#endregion
	}
}