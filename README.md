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
All remote method calls are synchronous, unless they return a Task/Task<T> or are attributed with the [AsyncRemote] attribute. The latter can only be attributed to method calls with a Void return type.

**How are concurrent calls on the same object handled?**
By default, method calls are dispatched using TaskScheduler.Default and thus may be invoked in parallel (if called at the same time).

**Can I specify the degree of parallelism to which method calls are invoked?**
Yes. This can be done by attributing the method with the [Invoke] attribute. The degree can be limited to "per-method", "per-object" and "per-type".

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
