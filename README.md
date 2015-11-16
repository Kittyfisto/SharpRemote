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
