# SharpRemote

[![Build status](https://ci.appveyor.com/api/projects/status/e4s3he430y1a27cb?svg=true)](https://ci.appveyor.com/project/Kittyfisto/sharpremote)
[![NuGet](https://img.shields.io/nuget/dt/sharpremote.svg)](http://nuget.org/packages/sharpremote)
[![NuGet](https://img.shields.io/nuget/v/sharpremote.svg)](http://nuget.org/packages/sharpremote)

SharpRemote is a free and active open-source project aimed at developing distributed applications that run accross different processes, machines and networks.

SharpRemote is supported on Windows 7, 8 and 10 and requires .NET 4.5 or higher.

Contrary to other solutions, this project does NOT require an [IDL](https://en.wikipedia.org/wiki/Interface_description_language). Instead,
all remoting behaviour is defined directly via .NET types and attributes where needed.

** This library is not intended for production code yet **

## Installation

1. Install latest nuget package (https://www.nuget.org/packages/SharpRemote/)
2. Done

## Soundbites

- SharpRemote is a framework to enable you to build .NET applications spawning multiple processes, machines and networks
- All communication happens via TCP/IP connections
- Failure recovery

## Supported scenarios

### 1. Hosting (unreliable code) out of process

As is often the case, an application must make use of third party code which exhibits unreliable behaviour. It may consume too many resources,
cause access violations or even call exit() when it's inconvenient. SharpRemote makes it **incredibly** easy to host these portions outside
your own process so that **most** failures become recoverable.

The following examples causes a new process to be spawned. In this new process, a new object of type `SomeInterfaceImplementation` is created.
The caller receives a so called proxy object which also implements `ISomeInterface`. Finally, Do() is invoked on the proxy which causes
Do() to be invoked on the actual object of the remote process *synchronously*.

**Example**:
```
public interace ISomeInterface
{
	void Do();
}
...
var silo = new OutOfProcessSilo();
silo.Start();
var @interface = silo.CreateGrain<ISomeInterface>(typeof(SomeInterfaceImplementation));
@interface.Do();
```

By default, `OutOfProcessSilo` will simply restart the remote process in case of failures (such as the process crashing / not responding anymore).
However as usual, you can fine tune this behaviour via its configuration and `IFailureHandler` implementations.

### 2. Client-Server application over LAN/WAN

SharpRemote allows you to develop a typical client/server application using only .NET. All communication between client and server is performed
via SharpRemote: All you have to worry about is to define the interface over which the communication is performed:

**Interface:**
```
[DataContract]
public struct Document
{
	[DataMember]
	public int Id{get;set;}

	[DataMember]
	public string Name{get;set;}

	[DataMember]
	public string Content{get;set;}
}

public interface IDocuments
{
	Task<Document[]> GetAllAsync();
	Task<Document> GetDocumentByIdAsync(int id);
	Task PutAsync(Document document);
}
...
const int DocumentsIdentifier = 9001;
```

**Server:**
```
class Documents : IDocuments
{
...
}
var documents = new Documents();
var endPoint = new SocketServer();
endPoint.RegisterSubject<IDocuments>(DocumentsIdentifier, documents);
endPoint.Bind(IPAddress.Any);
```

**Client:**
```
var endPoint = new SocketEndPoint(EndPointType.Client);
var documents = endPoint.CreateProxy<IDocuments>(DocumentsIdentifier);
...
documents.PutAsync(new Document());
```

## Q&A

**Are remote method calls synchronous or asynchronous?**  
All remote method calls are synchronous, unless they return a Task/Task&lt;T&gt; or are attributed with the [AsyncRemote] attribute. The latter can only be attributed to method calls with a Void return type.

**When should I use synchronous/asynchronous calls?**  
Synchronous code is easier to write, understand and to maintain and thus should be preferred. Unless absolutely necessary, synchronous code should be your go-to solution, especially if your asynchronous code is as follows:

```c#
var task = myProxy.DoStuff();  
task.Wait();  
```

Asynchronous method calls may improve your performance tremendously, but this depends on the amount of calls, as well as the latency involved. In general, the higher the latency or the higher the amount of calls per second, the more benefit you get from switching to asynchronous invocations.  
As always, **measure first, optimize later**.

**How are concurrent calls on the same object handled?**  
By default, method calls are dispatched using TaskScheduler.Default and thus may be invoked in parallel (if called at the same time).

**Can I specify the degree of parallelism to which method calls are invoked?**  
Yes. This can be done by attributing the method with the [Invoke] attribute. The degree can be limited to "per-method", "per-object" and "per-type".

**How are methods executed?**  
SharpRemote promises that each and every remote call is either eventually executed or an exception is thrown in case a failure occured.
Individual remote method calls can never time out: This means that if a non-async method blocks for an hour, then its caller will be stuck for an hour (unless a failure occured, see below). If you specifically want methods to time out, then the remoting interface should be changed to use asynchronous methods (those which return Task/Task<T>).

**How are unhandled exceptions handled?**
If a method throws an exception, then said exception is serialized and re-thrown on the caller's side. For synchronous methods, this means that the method call throws an exception, for asynchronous methods, the returned task will fail and return the original exception.
Please note that exceptions thrown by synchronous method calls are currently NOT wrapped in an AggregateException. If an exception isn't serializable, then an UnserializableException with the original message is thrown instead. See https://blogs.msdn.microsoft.com/agileer/2013/05/17/the-correct-way-to-code-a-custom-exception-class/ for how to write a custom exception which can be serialized.

**How are failures handled?**
SharpRemote monitors the health of the entire connection: If the other endpoint stops processing messages, or the underlying connection (currently only a socket is used) is disconnected, then it is assumed that the connection is dead and must be disconnected. All pending or currently executing remote method calls throw the following exceptions on their calling thread:

*SharpRemote.ConnectionLostException*  
The connection was interrupted **while** the method call occured **or** was pending. The method may or may not have been executed in the remote process.

*SharpRemote.NotConnectedException*
The method call was performed *after* a connection was lost or *before* a connection was established. Either way the method was definately not executed in the remote process.

**How can I avoid running into "failures" introduced by pausing the involved processes with a debugger?**  
You should set HeartbeatSettings.ReportSkippedHeartbeatsAsFailureWithDebuggerAttached, HeartbeatSettings.ReportDebuggerAttached and
HeartbeatSettings.ReportSkippedHeartbeatsAsFailureWithDebuggerAttached to true. Doing so will allow you to attach a debugger to one process which will in turn tell the other endpoint to disable timeout detecting until the debugger is detached again. You shouldn't do this in production environments however, as at will allow malicious clients to consume server resources without ever getting disconnected.
For the sake of completion, it is possible, but heavily discouraged as per reason above, to disable timeout detection using HeartbeatSettings.Dont.

**What types are supported for serialization?**  
A lot of native .NET types are supported out of the box (integer, floating-point, string, datetime, etc...). User defined types must either be attributed with the [ByReference] or [DataContract] attribute. The latter requires all fields / properties that shall be serializable be marked with the [DataMember] attribute.

**What does the DataContract attribute imply?**  
The object (be it derrived from object or ValueType) is serialized as a value: (backing) field for field. Only fields and/or properties attributed with the [DataMember] attribute are serialized, all others are skipped.
Calling the same method with a value-type object as the only parameter twice results in the entire object-graph to be serialized twice, once for each method call.

**What does the ByReference attribute imply?**  
The object-graph is never serialized: passing such an object into a method causes the invoked method to be passed either a new proxy or be passed an existing proxy that *references* said object. Accessing any property of such a proxy-object results in its own remote method call.

**What's a proxy?**  
A proxy is an object that presents an identical interface to its subject, but is, in fact, a different object. To any outsider, however, the proxy is virtually indistinguishable from the subject itself. In SharpRemote a proxy object represents an object on a different process, machine and/or network: Method calls to proxy result in the corresponding to be invoked on the subject - event invocations on the subject are invoked on the proxy.

**So proxies are allocated on demand for reference types - when are they destroyed?**  
As answered previously, proxies are automatically generated when a [ByReference] object is used as a parameter and/or return value. Proxy objects will be automatically collected by the garbage collector when they are no longer reachable.

**Can non-custom classes be serialized by reference?**  
No. Currently, a class or interface must be attributed with the [ByReference] attribute in order to introduce said behaviour.
Contact me if this is an essential feature for you.

**How do you handle polymorphism?**  
When an object is serialized, then its true type is queried and then its serialization behaviour is looked up (this lookup happens in constant time for every lookup besides the first one). If the type happens to implement an interface which is attributed with the [ByReference] attribute, then the object is serialized by reference. If it's attributed with the [DataContract] attribute or it is a built-in type, then it is serialized by value. Otherwise an exception is thrown at runtime.
If you have an interface such as the following:

    interface IFoo
    {
        void Process(object data);
    }

Then invoking it as follows:

    foo.Process(42);
    foo.Process(DateTime.UtcNow);

Will just work as expected.
However if you pass an object which is not serializable, then an ArgumentException is thrown:

    foo.Process(Thread.CurrentThread);

Please note that this behaviour is identical for both synchronous as well as asynchronous method calls.

## Contributing

1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :D

## History

TODO: Write history

## Credits

TODO

## License

[MIT](http://opensource.org/licenses/MIT)
