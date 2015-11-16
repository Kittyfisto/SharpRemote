# SharpRemote

[![Build status](https://ci.appveyor.com/api/projects/status/e4s3he430y1a27cb?svg=true)](https://ci.appveyor.com/project/Kittyfisto/sharpremote)

SharpRemote is a free and active open-source project aimed at developing distributed applications that run accross different processes and/or machines.

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

**How are values serialized?**  
Values, e.g. types which derrive from ValueType, are always serialized by value; that is field by field. This behaviour cannot be changed.

**How are classes serialized?**  
Identical to how values are serialized, e.g. by value. This bevahiour can be configured by attributing the class in question (or any of its sub-types) with the [ByReference] attribute. 

**Can custom classes be serialized by reference?**  
Not yet. It will be implemented as soon as there is demand for it (contact me).

**How do you handle polymorphism?**  
A method which takes an object parameter behaves identical to a method which takes a Foo parameter, if both are called with Foo-objects. The former may perform slightly worse due to having to query the object's type first.

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
