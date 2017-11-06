using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SharpRemote.Watchdog
{
	internal sealed class IsolatedStorage
		: IIsolatedStorage
	{
		public const string AppName = "SharpRemote.Watchdog";

		public T Restore<T>(string name)
		{
			var path = GetPath(name);
			if (!File.Exists(path))
				return default(T);

			using (var stream = File.OpenRead(path))
			{
				var serializer = new System.Xml.Serialization.XmlSerializer(typeof (T));
				return (T)serializer.Deserialize(stream);
			}
		}

		private string GetPath(string name)
		{
			var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var path = Path.Combine(folder, AppName, name);
			return path;
		}

		public void Store<T>(string name, T value)
		{
			var path = GetPath(name);
			Directory.CreateDirectory(path);

			if (File.Exists(path))
				File.Delete(path);

			if (value != null)
			{
				using (var stream = File.OpenWrite(path))
				{
					var writer = new XmlTextWriter(stream, Encoding.UTF8)
					{
						IndentChar = '\t',
						Indentation = 1
					};
					var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
					serializer.Serialize(writer, value);
				}
			}
		}
	}
}