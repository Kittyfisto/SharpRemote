using System;

namespace SharpRemote
{
	/// <summary>
	///     Base class for <see cref="IMethodInvocationWriter" /> implementations which do not want to
	///     provide overwrites for .NET's pod types (and don't care about the additional boxing effort involved).
	/// </summary>
	public abstract class AbstractMethodInvocationWriter
		: IMethodInvocationWriter
	{
		/// <inheritdoc />
		public abstract void Dispose();

		/// <inheritdoc />
		public void WriteNamedArgument(string name, object value)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, sbyte value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, byte value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, ushort value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, short value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, uint value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, int value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, ulong value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, long value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, float value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, double value)
		{
			WriteNamedArgument(name, (object) value);
		}

		/// <inheritdoc />
		public void WriteNamedArgument(string name, string value)
		{
			WriteNamedArgument(name, (object) value);
		}
	}
}