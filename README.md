# SharpRemote

Master: [![Build status](https://ci.appveyor.com/api/projects/status/e4s3he430y1a27cb?svg=true)](https://ci.appveyor.com/project/Kittyfisto/sharpremote)  
Dev:    [![Build status](https://ci.appveyor.com/api/projects/status/8icg92xvgfhp1tnf?svg=true)](https://ci.appveyor.com/project/Kittyfisto/sharpremote-2j0wg)  

SharpRemote is a free and active open-source project aimed at developing distributed applications that run accross different processes, machines and networks.

SharpRemote is supported on Windows 7, 8 and 10 and requires .NET 4.5 or higher.

Instead of requring remote-able interface definitions to be written in an [IDL](https://en.wikipedia.org/wiki/Interface_description_language), SharpRemote works directly with any .NET inferface definition and generates proxy / servant implementations at runtime on-demand.

** This library is not intended for production code yet **

## Installation

1. Install latest nuget package (https://www.nuget.org/packages/SharpRemote/)
2. Done

## Usage

1. Define a c# interface that represents all possible communication between its user and implementation
2. Create an ISilo implementation of your choice, that is responsible for hosting the object (for example ProcessSilo to host the object in a different process)
3. Call ISilo.Create<T>(Type) in order to create an instance of 'Type', offering interface 'T' for communication
4. Use the interface in order to interact with the newly created object

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

**How are failures handled?**  
SharpRemote promises that each and every remote method call is eventually executed. Individual remote method calls can never time out, instead the health of the entire connection is used to determine whether a failure occured. A connection is said to have failed when the underlying socket reports a failure or when the connection doesn't process any method call for a certain amount of time.
As soon as a failure happens, SharpRemote tears down the connection and notifies all pending or currently executing calls by throwing the following exceptions on the calling thread:

*SharpRemote.ConnectionLostException*  
The connection was interrupted **while** the method call occured **or** was pending. The method may or may not have been executed in the remote process.

*SharpRemote.NotConnectedException*
The method call was performed *after* a connection was lost or *before* a connection was established. Either way the method was definately not executed in the remote process.

**How can I avoid running into "failures" introduced by pausing the involved processes with a debugger?**  
You can either deactivate the timeout detection of a connection completely by setting the HeartbeatSettings.UseHeartbeatForFaultDetection property to false or by attaching a debugger to **every** process involved and setting the HeartbeatSettings.ReportSkippedHeartbeatsAsFailureWithDebuggerAttached property to false.

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
An object's true type is queried (in case the method parameter / return type is non sealed) and then dynamic dispatch (if necessary) is used to invoke the class'es serialization behaviour.

## Samples

TODO

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
