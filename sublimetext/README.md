## FSharp - An F# Package for Sublime Text

This package provides support for F# development in Sublime Text.

FSharp is currently a preview and not ready for use. If you want to
contribute to its development, you can read on to learn how to set up a
development environment.


### Developing FSharp

Pull requests to FSharp are welcome.

FSharp is only compatible with **Sublime Text 3**.


#### General steps

* Clone this repository to any folder outside of Sublime Text's *Data* folder
* Edit files as needed
* Publish the project using `make install` or `.bin/Build.ps1`
* Restart Sublime Text
* Run the tests via command palette: *FSharp: Run Tests*


#### Building

See below for platform-specific instructions.


### Development environment - Linux/Mac

Copy *FSharp*'s content manually to *{Sublime Text Data Path}/Packages/FSharp*.


### Development environment - Windows

#### Requirements


You must set `$STDataPath` in your PowerShell session to Sublime Text's *Data* path.

Build process:

Run `.\bin\GetDependencies.ps1` once to get dependencies.
Run `.\bin\Build.ps1` to build and publish the files locally.
