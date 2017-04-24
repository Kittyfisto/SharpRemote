using System;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using SharpRemote.WebApi.Requests;

namespace SharpRemote.WebApi.HttpListener
{
	/// <summary>
	/// </summary>
	public sealed class SystemNetHttpListener
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly System.Net.HttpListener _listener;
		private readonly IRequestHandler _handler;
		private bool _isDisposed;

		/// <summary>
		/// </summary>
		public SystemNetHttpListener(IRequestHandler handler)
			: this(CreateListener(), handler)
		{
		}

		/// <summary>
		/// </summary>
		/// <param name="listener"></param>
		/// <param name="handler"></param>
		public SystemNetHttpListener(System.Net.HttpListener listener, IRequestHandler handler)
		{
			if (listener == null)
				throw new ArgumentNullException(nameof(listener));
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			_listener = listener;
			_handler = handler;

			Listen();
		}
		
		private void Listen()
		{
			try
			{
				_listener.BeginGetContext(OnContext, null);
			}
			catch (ObjectDisposedException e)
			{
				// This exception is expected because we don't synchronize dispose with
				// BeginGetContext...
			}
			catch (Exception e)
			{
				Log.Error("Caught unexpected exception: {0}", e);
			}
		}

		private void OnContext(IAsyncResult ar)
		{
			try
			{
				var context = _listener.EndGetContext(ar);
				var request = new WebRequestContext(context);
				Task.Factory.StartNew(() => Run(request));
			}
			catch (ObjectDisposedException e)
			{
				// This exception is expected because we don't synchronize dispose with
				// EndGetContext...
			}
			catch (Exception e)
			{
				Log.Error("Caught unexpected exception: {0}", e);
			}
			finally
			{
				if (!_isDisposed)
					Listen();
			}
		}

		private void Run(WebRequestContext context)
		{
			WebResponse response;
			try
			{
				var uri = context.Request.Url;
				var subUri = RemovePrefix(uri);
				response = _handler.TryHandle(subUri, context.Request);
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception: {0}", e);
				response = new WebResponse(500);
			}
			context.SetResponse(response);
		}

		private string RemovePrefix(Uri uri)
		{
			var url = uri.ToString();
			foreach (var prefix in _listener.Prefixes)
			{
				if (url.StartsWith(prefix))
				{
					return url.Substring(prefix.Length);
				}
			}

			return url;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_isDisposed = true;
			((IDisposable) _listener)?.Dispose();
		}
		
		private static System.Net.HttpListener CreateListener()
		{
			return new System.Net.HttpListener();
		}
	}
}