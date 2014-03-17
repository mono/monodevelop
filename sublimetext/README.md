## FSharp - F# Support for Sublime Text

This package provides support for F# development in Sublime Text.

FSharp is currently a preview and not ready for use. If you want to
contribute to its development, you can read on to learn how to set up a
development environment.


### Developing FSharp

Pull requests to FSharp are welcome.

At the moment, FSharp is only compatible with Sublime Text 3.

See also *FSharp_Tests/README.md*.

#### General steps

* Clone this repository to any folder outside of Sublime Text's *Data* folder
* Edit files as needed
* Edit tests in FSharp_Tests as needed
* Publish the project using `make install` or `.bin/Publish.ps1`
* Restart Sublime Text
* Run the tests via command palette: *FSharp: Run Tests*


#### Building

See below for platform-specific instructions.


### Development environment - Linux/Mac

#### Requirements

* Python 2.7 or above (including Python 3)

Run `make install` to obtain dependencies and publish the files locally.
Run `make build` to only publish the files locally.

Check the *Makefile* for more options.


### Development environment - Windows

#### Requirements

* Python 2.7 or above (including Python 3)

If you're using a **portable installation** of Sublime Text, you must set
`$STDataPath` in your PowerShell session to Sublime Text's *Data* path. For
**full installations**, the build script will attempt to find said directory
automatically.

Build process:

Run `.\bin\GetDependencies.ps1` once to get dependencies.
Run `.\bin\Publish.ps1` to publish the files locally.
