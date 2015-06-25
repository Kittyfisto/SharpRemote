using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when the exception thrown by the servant could not be serialized.
	/// It preserves a lot of information about the original exception to help find the original cause
	/// for it.
	/// </summary>
	[Serializable]
	public class UnserializableException
		: RemotingException
	{
		private readonly string _originalMessage;
		private readonly string _originalSource;
		private readonly string _originalStacktrace;

#if !WINDOWS_PHONE_APP
		private readonly string _originalTargetSite;
#endif

		private readonly string _originalTypename;

		public UnserializableException(Exception originalException)
		{
			_originalMessage = originalException.Message;
			_originalStacktrace = originalException.StackTrace;
			_originalTypename = originalException.GetType().AssemblyQualifiedName;
			_originalSource = originalException.Source;

#if !WINDOWS_PHONE_APP
			_originalTargetSite = originalException.TargetSite.Name;
#endif

			HResult = originalException.HResult;
		}

#if !WINDOWS_PHONE_APP
		public UnserializableException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			_originalMessage = info.GetString("OriginalMessage");
			_originalStacktrace = info.GetString("OriginalStacktrace");
			_originalTypename = info.GetString("OriginalExceptionType");
			_originalSource = info.GetString("OriginalSource");
			_originalTargetSite = info.GetString("OriginalTargetSite");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("OriginalMessage", _originalMessage);
			info.AddValue("OriginalStacktrace", _originalStacktrace);
			info.AddValue("OriginalExceptionType", _originalTypename);
			info.AddValue("OriginalSource", _originalSource);
			info.AddValue("OriginalTargetSite", _originalTargetSite);
		}
#endif

        public string OriginalMessage
		{
			get { return _originalMessage; }
		}

		public string OriginalStacktrace
		{
			get { return _originalStacktrace; }
		}

		public string OriginalSource
		{
			get { return _originalSource; }
		}

		public string OriginalTypename
		{
			get { return _originalTypename; }
		}

#if !WINDOWS_PHONE_APP
		public string OriginalTargetSite
		{
			get { return _originalTargetSite; }
		}
#endif
	}
}