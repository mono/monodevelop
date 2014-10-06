# F# Language Support for Open Editors

This project contains advanced editing support for F# for a number of open editors. It is made up of the following projects:
* [F# mode for Emacs](emacs/README.md)
* [F# addin for MonoDevelop and Xamarin Studio](monodevelop/README.md)
* [FSharp.AutoComplete](FSharp.AutoComplete/README.md)

Rich editing (intellisense) support for [Sublime Text](../sublimetext) and [Vim](https://github.com/kjnilsson/fsharp-vim) is also under development.

If you are interested in adding rich editor support for another editor, please open an [issue](https://github.com/fsharp/fsharpbinding/issues) to kick-start the discussion.

See the [F# Cross-Platform Development Guide](http://fsharp.org/guides/mac-linux-cross-platform/index.html#editing) for F# with Sublime Text 2, Vim and other editors not covered here.

## Build Status

The CI builds are handled by a [FAKE script](FSharp.AutoComplete/build.fsx), which:

* Builds FSharp.AutoComplete
* Runs FSharp.AutoComplete unit tests
* Runs FSharp.AutoComplete integration tests
* Runs Emacs unit tests
* Runs Emacs integration tests
* Runs Emacs byte compilation

### Travis [![Travis build status](https://travis-ci.org/fsharp/fsharpbinding.png)](https://travis-ci.org/fsharp/fsharpbinding)

See [.travis.yml](.travis.yml) for details.

### AppVeyor [![AppVeyor build status](https://ci.appveyor.com/api/projects/status/plirrv4behpjrqo8)](https://ci.appveyor.com/project/rneatherway/fsharpbinding-243)

The configuration is contained in [appveyor.yml](appveyor.yml). Currently the emacs integration tests do not run successfully on AppVeyor and are excluded by the FAKE script.

## Building, using and contributing

See the README for each individual component:

* [monodevelop/README.md](monodevelop/README.md)
* [fsautocomplete](FSharp.AutoComplete/README.md)
* [emacs](emacs/README.md)

## Shared Components

The core shared component is FSharp.Compiler.Service.dll from the 
community [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service) project.
This is used by both [fsautocomplete.exe](https://github.com/fsharp/fsharpbinding/tree/master/FSharp.AutoComplete), 
a command-line utility to sit behind Emacs, Vim and other editing environments components. 

For more information about F# see [The F# Software Foundation](http://fsharp.org). Join [The F# Open Source Group](http://fsharp.github.com). We use [github](https://github.com/fsharp/fsharpbinding) for tracking work items and suggestions.
