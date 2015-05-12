using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public partial class SerializationTest
	{
		[Test]
		public void TestKeyValuePair()
		{
			_serializer.ShouldRoundtrip(new KeyValuePair<int, string>(42, "FOobar"));
			_serializer.ShouldRoundtrip(new KeyValuePair<int, KeyValuePair<string, object>>(42, new KeyValuePair<string, object>("Foobar", typeof(int))));
		}

		[Test]
		public void TestType()
		{
			_serializer.RegisterType<Type>();
			_serializer.ShouldRoundtrip(typeof(int));
		}

		[Test]
		public void TestIPAddress()
		{
			_serializer.RegisterType<IPAddress>();
			_serializer.ShouldRoundtrip(IPAddress.Parse("192.168.0.87"));
			_serializer.ShouldRoundtrip(IPAddress.IPv6Loopback);
		}

		[Test]
		public void TestIPEndPoint()
		{
			var ep = new IPEndPoint(IPAddress.Parse("192.168.0.87"), 80);
			_serializer.ShouldRoundtrip(ep);

			ep = new IPEndPoint(IPAddress.IPv6Loopback, 55980);
			_serializer.ShouldRoundtrip(ep);
		}

		[Test]
		public void TestTimeSpan()
		{
			_serializer.ShouldRoundtrip(TimeSpan.Zero);
			_serializer.ShouldRoundtrip(TimeSpan.FromSeconds(1.5));
			_serializer.ShouldRoundtrip(TimeSpan.FromDays(4));
			_serializer.ShouldRoundtrip(TimeSpan.FromDays(-4));
			_serializer.ShouldRoundtrip(TimeSpan.MinValue);
			_serializer.ShouldRoundtrip(TimeSpan.MaxValue);
		}

		[Test]
		public void TestDateTime()
		{
			_serializer.ShouldRoundtrip(DateTime.Now);
			_serializer.ShouldRoundtrip(DateTime.UtcNow);
			_serializer.ShouldRoundtrip(new DateTime(2015, 5, 12, 20, 00, 23, DateTimeKind.Local));
			_serializer.ShouldRoundtrip(new DateTime(2015, 5, 12, 20, 00, 23, DateTimeKind.Unspecified));
			_serializer.ShouldRoundtrip(new DateTime(2015, 5, 12, 20, 00, 23, DateTimeKind.Utc));
			_serializer.ShouldRoundtrip(DateTime.MinValue);
			_serializer.ShouldRoundtrip(DateTime.MaxValue);
		}

		[Test]
		public void TestDateTimeOffset()
		{
			_serializer.ShouldRoundtrip(DateTimeOffset.Now);
			_serializer.ShouldRoundtrip(DateTimeOffset.UtcNow);
		}

		[Test]
		public void TestGuid()
		{
			_serializer.ShouldRoundtrip(new Guid("936DA01F-9ABD-4d9d-80C7-02AF85C822A8"));
			_serializer.ShouldRoundtrip(new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4"));
		}

		[Test]
		public void TestVersion()
		{
			_serializer.ShouldRoundtrip(new Version(4, 0, 3211, 45063));
			_serializer.ShouldRoundtrip(new Version(0, 0, 0, 0));
			_serializer.ShouldRoundtrip(new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));
		}

		[Test]
		public void TestContextForm()
		{
			_serializer.ShouldRoundtrip(ActivationContext.ContextForm.Loose);
			_serializer.ShouldRoundtrip(ActivationContext.ContextForm.StoreBounded);
		}

		[Test]
		public void TestAppDomainManagerInitializationOptions()
		{
			_serializer.ShouldRoundtrip(AppDomainManagerInitializationOptions.None);
			_serializer.ShouldRoundtrip(AppDomainManagerInitializationOptions.RegisterWithHost);
		}

		[Test]
		public void TestAttributeTargets()
		{
			_serializer.ShouldRoundtrip(AttributeTargets.All);
			_serializer.ShouldRoundtrip(AttributeTargets.Assembly);
			_serializer.ShouldRoundtrip(AttributeTargets.Class);
			_serializer.ShouldRoundtrip(AttributeTargets.Constructor);
			_serializer.ShouldRoundtrip(AttributeTargets.Delegate);
			_serializer.ShouldRoundtrip(AttributeTargets.Enum);
			_serializer.ShouldRoundtrip(AttributeTargets.Event);
			_serializer.ShouldRoundtrip(AttributeTargets.Field);
			_serializer.ShouldRoundtrip(AttributeTargets.GenericParameter);
			_serializer.ShouldRoundtrip(AttributeTargets.Interface);
			_serializer.ShouldRoundtrip(AttributeTargets.Method);
			_serializer.ShouldRoundtrip(AttributeTargets.Module);
			_serializer.ShouldRoundtrip(AttributeTargets.Parameter);
			_serializer.ShouldRoundtrip(AttributeTargets.Property);
			_serializer.ShouldRoundtrip(AttributeTargets.ReturnValue);
			_serializer.ShouldRoundtrip(AttributeTargets.Struct);
		}

		[Test]
		public void TestBase64FormattingOptions()
		{
			_serializer.ShouldRoundtrip(Base64FormattingOptions.InsertLineBreaks);
			_serializer.ShouldRoundtrip(Base64FormattingOptions.None);
		}

		[Test]
		public void TestConsoleColor()
		{
			_serializer.ShouldRoundtrip(ConsoleColor.Black);
			_serializer.ShouldRoundtrip(ConsoleColor.Blue);
			_serializer.ShouldRoundtrip(ConsoleColor.Cyan);
			_serializer.ShouldRoundtrip(ConsoleColor.DarkBlue);
			_serializer.ShouldRoundtrip(ConsoleColor.DarkCyan);
			_serializer.ShouldRoundtrip(ConsoleColor.DarkGray);
			_serializer.ShouldRoundtrip(ConsoleColor.DarkGreen);
			_serializer.ShouldRoundtrip(ConsoleColor.DarkMagenta);
			_serializer.ShouldRoundtrip(ConsoleColor.DarkRed);
			_serializer.ShouldRoundtrip(ConsoleColor.DarkYellow);
			_serializer.ShouldRoundtrip(ConsoleColor.Gray);
			_serializer.ShouldRoundtrip(ConsoleColor.Green);
			_serializer.ShouldRoundtrip(ConsoleColor.Magenta);
			_serializer.ShouldRoundtrip(ConsoleColor.Red);
			_serializer.ShouldRoundtrip(ConsoleColor.White);
			_serializer.ShouldRoundtrip(ConsoleColor.Yellow);
		}

		[Test]
		public void TestConsoleModifiers()
		{
			_serializer.ShouldRoundtrip(ConsoleModifiers.Alt);
			_serializer.ShouldRoundtrip(ConsoleModifiers.Control);
			_serializer.ShouldRoundtrip(ConsoleModifiers.Shift);
		}

		[Test]
		public void TestConsoleSpecialKey()
		{
			_serializer.ShouldRoundtrip(ConsoleSpecialKey.ControlBreak);
			_serializer.ShouldRoundtrip(ConsoleSpecialKey.ControlC);
		}

		[Test]
		public void TestDateTimeKind()
		{
			_serializer.ShouldRoundtrip(DateTimeKind.Local);
			_serializer.ShouldRoundtrip(DateTimeKind.Unspecified);
			_serializer.ShouldRoundtrip(DateTimeKind.Utc);
		}

		[Test]
		public void TestDayOfWeek()
		{
			_serializer.ShouldRoundtrip(DayOfWeek.Friday);
			_serializer.ShouldRoundtrip(DayOfWeek.Monday);
			_serializer.ShouldRoundtrip(DayOfWeek.Saturday);
			_serializer.ShouldRoundtrip(DayOfWeek.Sunday);
			_serializer.ShouldRoundtrip(DayOfWeek.Thursday);
			_serializer.ShouldRoundtrip(DayOfWeek.Tuesday);
			_serializer.ShouldRoundtrip(DayOfWeek.Wednesday);
		}

		[Test]
		public void TestSpecialFolder()
		{
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.AdminTools);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.ApplicationData);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CDBurning);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonAdminTools);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonApplicationData);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonDesktopDirectory);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonDocuments);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonMusic);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonOemLinks);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonPictures);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonProgramFiles);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonProgramFilesX86);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonPrograms);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonStartMenu);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonStartup);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonTemplates);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.CommonVideos);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Cookies);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Desktop);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.DesktopDirectory);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Favorites);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Fonts);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.History);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.InternetCache);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.LocalApplicationData);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.LocalizedResources);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.MyComputer);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.MyDocuments);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.MyMusic);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.MyPictures);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.MyVideos);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.NetworkShortcuts);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Personal);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.PrinterShortcuts);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.ProgramFiles);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.ProgramFilesX86);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Programs);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Recent);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Resources);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.SendTo);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.StartMenu);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Startup);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.System);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.SystemX86);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Templates);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.UserProfile);
			_serializer.ShouldRoundtrip(Environment.SpecialFolder.Windows);
		}

		[Test]
		public void TestSpecialFolderOption()
		{
			_serializer.ShouldRoundtrip(Environment.SpecialFolderOption.Create);
			_serializer.ShouldRoundtrip(Environment.SpecialFolderOption.DoNotVerify);
			_serializer.ShouldRoundtrip(Environment.SpecialFolderOption.None);
		}

		[Test]
		public void TestEnvironmentVariableTarget()
		{
			_serializer.ShouldRoundtrip(EnvironmentVariableTarget.Machine);
			_serializer.ShouldRoundtrip(EnvironmentVariableTarget.Process);
			_serializer.ShouldRoundtrip(EnvironmentVariableTarget.User);
		}

		[Test]
		public void TestGCCollectionMode()
		{
			_serializer.ShouldRoundtrip(GCCollectionMode.Default);
			_serializer.ShouldRoundtrip(GCCollectionMode.Forced);
			_serializer.ShouldRoundtrip(GCCollectionMode.Optimized);
		}

		[Test]
		public void TestGCNotificationStatus()
		{
			_serializer.ShouldRoundtrip(GCNotificationStatus.Canceled);
			_serializer.ShouldRoundtrip(GCNotificationStatus.Failed);
			_serializer.ShouldRoundtrip(GCNotificationStatus.NotApplicable);
			_serializer.ShouldRoundtrip(GCNotificationStatus.Succeeded);
			_serializer.ShouldRoundtrip(GCNotificationStatus.Timeout);
		}

		[Test]
		public void TestGenericUriParserOptions()
		{
			_serializer.ShouldRoundtrip(GenericUriParserOptions.AllowEmptyAuthority);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.Default);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.DontCompressPath);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.DontConvertPathBackslashes);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.DontUnescapePathDotsAndSlashes);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.GenericAuthority);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.Idn);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.IriParsing);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.NoFragment);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.NoPort);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.NoQuery);
			_serializer.ShouldRoundtrip(GenericUriParserOptions.NoUserInfo);
		}

		[Test]
		public void TestLoaderOptimization()
		{
#pragma warning disable 612,618
			_serializer.ShouldRoundtrip(LoaderOptimization.DisallowBindings);
			_serializer.ShouldRoundtrip(LoaderOptimization.DomainMask);
#pragma warning restore 612,618

			_serializer.ShouldRoundtrip(LoaderOptimization.MultiDomain);
			_serializer.ShouldRoundtrip(LoaderOptimization.MultiDomainHost);
			_serializer.ShouldRoundtrip(LoaderOptimization.NotSpecified);
			_serializer.ShouldRoundtrip(LoaderOptimization.SingleDomain);
		}

		[Test]
		public void TestMidpointRounding()
		{
			_serializer.ShouldRoundtrip(MidpointRounding.AwayFromZero);
			_serializer.ShouldRoundtrip(MidpointRounding.ToEven);
		}

		[Test]
		public void TestPlatformId()
		{
			_serializer.ShouldRoundtrip(PlatformID.MacOSX);
			_serializer.ShouldRoundtrip(PlatformID.Unix);
			_serializer.ShouldRoundtrip(PlatformID.Win32NT);
			_serializer.ShouldRoundtrip(PlatformID.Win32S);
			_serializer.ShouldRoundtrip(PlatformID.Win32Windows);
			_serializer.ShouldRoundtrip(PlatformID.WinCE);
			_serializer.ShouldRoundtrip(PlatformID.Xbox);
		}

		[Test]
		public void TestStringComparison()
		{
			_serializer.ShouldRoundtrip(StringComparison.CurrentCulture);
			_serializer.ShouldRoundtrip(StringComparison.CurrentCultureIgnoreCase);
			_serializer.ShouldRoundtrip(StringComparison.InvariantCulture);
			_serializer.ShouldRoundtrip(StringComparison.InvariantCultureIgnoreCase);
			_serializer.ShouldRoundtrip(StringComparison.Ordinal);
			_serializer.ShouldRoundtrip(StringComparison.OrdinalIgnoreCase);
		}

		[Test]
		public void TestStringSplitOptions()
		{
			_serializer.ShouldRoundtrip(StringSplitOptions.None);
			_serializer.ShouldRoundtrip(StringSplitOptions.RemoveEmptyEntries);
		}

		[Test]
		public void TestTypeCode()
		{
			_serializer.ShouldRoundtrip(TypeCode.Boolean);
			_serializer.ShouldRoundtrip(TypeCode.Byte);
			_serializer.ShouldRoundtrip(TypeCode.Char);
			_serializer.ShouldRoundtrip(TypeCode.DateTime);
			_serializer.ShouldRoundtrip(TypeCode.DBNull);
			_serializer.ShouldRoundtrip(TypeCode.Decimal);
			_serializer.ShouldRoundtrip(TypeCode.Double);
			_serializer.ShouldRoundtrip(TypeCode.Empty);
			_serializer.ShouldRoundtrip(TypeCode.Int16);
			_serializer.ShouldRoundtrip(TypeCode.Int32);
			_serializer.ShouldRoundtrip(TypeCode.Int64);
			_serializer.ShouldRoundtrip(TypeCode.Object);
			_serializer.ShouldRoundtrip(TypeCode.SByte);
			_serializer.ShouldRoundtrip(TypeCode.String);
			_serializer.ShouldRoundtrip(TypeCode.UInt16);
			_serializer.ShouldRoundtrip(TypeCode.UInt32);
			_serializer.ShouldRoundtrip(TypeCode.UInt64);
		}

		[Test]
		public void TestUriComponents()
		{
			_serializer.ShouldRoundtrip(UriComponents.AbsoluteUri);
			_serializer.ShouldRoundtrip(UriComponents.Fragment);
			_serializer.ShouldRoundtrip(UriComponents.Host);
			_serializer.ShouldRoundtrip(UriComponents.HostAndPort);
			_serializer.ShouldRoundtrip(UriComponents.HttpRequestUrl);
			_serializer.ShouldRoundtrip(UriComponents.KeepDelimiter);
			_serializer.ShouldRoundtrip(UriComponents.NormalizedHost);
			_serializer.ShouldRoundtrip(UriComponents.Path);
			_serializer.ShouldRoundtrip(UriComponents.PathAndQuery);
			_serializer.ShouldRoundtrip(UriComponents.Port);
			_serializer.ShouldRoundtrip(UriComponents.Query);
			_serializer.ShouldRoundtrip(UriComponents.Scheme);
			_serializer.ShouldRoundtrip(UriComponents.SchemeAndServer);
			_serializer.ShouldRoundtrip(UriComponents.SerializationInfoString);
			_serializer.ShouldRoundtrip(UriComponents.StrongAuthority);
			_serializer.ShouldRoundtrip(UriComponents.StrongPort);
			_serializer.ShouldRoundtrip(UriComponents.UserInfo);
		}

		[Test]
		public void TestUriFormat()
		{
			_serializer.ShouldRoundtrip(UriFormat.SafeUnescaped);
			_serializer.ShouldRoundtrip(UriFormat.Unescaped);
			_serializer.ShouldRoundtrip(UriFormat.UriEscaped);
		}

		[Test]
		public void TestUriHostNameType()
		{
			_serializer.ShouldRoundtrip(UriHostNameType.Basic);
			_serializer.ShouldRoundtrip(UriHostNameType.Dns);
			_serializer.ShouldRoundtrip(UriHostNameType.IPv4);
			_serializer.ShouldRoundtrip(UriHostNameType.IPv6);
			_serializer.ShouldRoundtrip(UriHostNameType.Unknown);
		}

		[Test]
		public void TestUriIdnScope()
		{
			_serializer.ShouldRoundtrip(UriIdnScope.All);
			_serializer.ShouldRoundtrip(UriIdnScope.AllExceptIntranet);
			_serializer.ShouldRoundtrip(UriIdnScope.None);
		}

		[Test]
		public void TestUriKind()
		{
			_serializer.ShouldRoundtrip(UriKind.Absolute);
			_serializer.ShouldRoundtrip(UriKind.Relative);
			_serializer.ShouldRoundtrip(UriKind.RelativeOrAbsolute);
		}

		[Test]
		public void TestUriPartial()
		{
			_serializer.ShouldRoundtrip(UriPartial.Authority);
			_serializer.ShouldRoundtrip(UriPartial.Path);
			_serializer.ShouldRoundtrip(UriPartial.Query);
			_serializer.ShouldRoundtrip(UriPartial.Scheme);
		}

		[Test]
		public void TestApplicationId()
		{
			var appId = new ApplicationId(new byte[512],
			                              "SharpRemote",
			                              new Version(1, 2, 3, 4),
			                              "x86",
			                              "de-de");
			_serializer.ShouldRoundtrip(appId);
		}

		[Test]
		public void TestUri()
		{
			_serializer.ShouldRoundtrip(new Uri("/wayneland.html", UriKind.Relative));
			_serializer.ShouldRoundtrip(new Uri("www.google.com:1234/wayneland.html"));
			_serializer.ShouldRoundtrip(new Uri("https://msdn.microsoft.com/en-us/library/System(v=vs.110).aspx"));
		}

		[Test]
		public void TestList()
		{
			_serializer.ShouldRoundtripEnumeration(new List<int> { 1, 2, 3, 4 });
			_serializer.ShouldRoundtripEnumeration(new List<int> { 1 });
			_serializer.ShouldRoundtripEnumeration(new List<int>());
			_serializer.ShouldRoundtripEnumeration(new List<int> { 9001, int.MinValue, int.MaxValue });
		}

		[Test]
		public void TestHashSet()
		{
			_serializer.ShouldRoundtripEnumeration(new HashSet<int>());
			_serializer.ShouldRoundtripEnumeration(new HashSet<int> { 1, 2, 3, 4 });

			var value = new HashSet<string>(
				Enumerable.Range(0, 10000)
				.Select(x => x.ToString(CultureInfo.InvariantCulture))
				);
			_serializer.ShouldRoundtripEnumeration(value);

			_serializer.ShouldRoundtripEnumeration(new HashSet<object>());
			_serializer.ShouldRoundtripEnumeration(new HashSet<object>
			{
				42,
				"Foobar",
				IPAddress.Parse("192.168.0.20")
			});
		}

		[Test]
		public void TestLinkedList()
		{
			_serializer.ShouldRoundtripEnumeration(new LinkedList<IPAddress>());

			var values = new LinkedList<IPAddress>();
			values.AddLast(IPAddress.Loopback);
			values.AddLast(IPAddress.IPv6Any);
			values.AddLast(IPAddress.IPv6Loopback);
			values.AddLast(IPAddress.IPv6None);
			values.AddLast(IPAddress.Any);
			values.AddLast(IPAddress.Broadcast);
			values.AddLast(IPAddress.None);

			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestSortedList()
		{
			_serializer.ShouldRoundtripEnumeration(new LinkedList<IPAddress>());

			var values = new SortedList<int, string>
			{
				{1, "Never happened"},
				{2, "Attach of the Clones"},
				{3, "Revenge of the Sith"},
				{4, "A new hope"},
				{5, "The empire strikes back"},
				{6, "Return of the jedi"},
			};
			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestStack()
		{
			_serializer.ShouldRoundtripEnumeration(new Stack<int>());

			var values = new Stack<int>();
			values.Push(1);
			values.Push(2);
			values.Push(4);
			values.Push(3);
			values.Push(5);
			values.Push(42);
			values.Push(9001);

			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestQueue()
		{
			_serializer.ShouldRoundtripEnumeration(new Queue<int>());

			var values = new Queue<int>();
			values.Enqueue(1);
			values.Enqueue(5);
			values.Enqueue(4);
			values.Enqueue(1);
			values.Enqueue(10);

			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestSortedSet()
		{
			_serializer.ShouldRoundtripEnumeration(new SortedSet<string>());
			_serializer.ShouldRoundtripEnumeration(new SortedSet<string>
			{
				"a", "b", "", "foobar", "wookie"
			});
		}

		[Test]
		public void TestSortedDictionary()
		{
			_serializer.ShouldRoundtripEnumeration(new SortedDictionary<int, string>());
			_serializer.ShouldRoundtripEnumeration(new SortedDictionary<int, string>
			{
				{1, "One"},
				{2, "Two"},
				{3, "Three"},
				{4, "Four"},
			});
			_serializer.ShouldRoundtripEnumeration(new SortedDictionary<int, string>
			{
				{4, "Four"},
				{3, "Three"},
				{2, "Two"},
				{1, "One"},
			});
		}

		[Test]
		public void TestDictionary()
		{
			_serializer.ShouldRoundtripEnumeration(new Dictionary<int, string>());
			_serializer.ShouldRoundtripEnumeration(new Dictionary<int, string>
			{
				{5, "Who"},
				{4, "let"},
				{3, "the"},
				{2, "dogs"},
				{1, "out"},
			});
		}
	}
}