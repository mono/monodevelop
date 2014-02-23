# F# Language Support for Open Editors

This project contains advanced editing support for F# for a number of open editors. It is made up of the following projects:
* [F# mode for Emacs](emacs/README.md)
* [F# addin for MonoDevelop and Xamarin Studio](monodevelop/README.md)
* Some reusable components shared by these (see below)

See the [F# Cross-Platform Development Guide](http://fsharp.org/guides/mac-linux-cross-platform/index.html#editing) for F# with Sublime Text 2, Vim and other editors not covered here.

## Build Status [![Build Status](https://travis-ci.org/fsharp/fsharpbinding.png)](https://travis-ci.org/fsharp/fsharpbinding)

The CI script builds FSharp.CompilerBinding.dll and fsautocomplete.exe. Integration and unit tests are run for fsautocomplete.exe alone and the emacs fsharp-mode (including full integration with fsautocomplete.exe). See [.travis.yml](.travis.yml) for details.

## Building and Using

See [emacs/README.md](emacs/README.md) or  [monodevelop/README.md](monodevelop/README.md)

## Shared Components

The core shared component is FSharp.Compiler.Service.dll from the 
community [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service) project. 
This is used by both [fsautocomplete.exe](https://github.com/fsharp/fsharpbinding/tree/master/FSharp.AutoComplete), 
a command-line utility to sit behind Emacs, Vim and other editing environments components. 

Building:

	make

This produces FSharp.AutoComplete/bin/Debug/fsautocomplete.exe. To understand how to use this component, see the other projects.

A component called FSharp.CompilerBinding.dll is also present, which is used for common functionality shared by the monodevelop binding and fsautocomplete.

For more information about F# see [The F# Software Foundation](http://fsharp.org). Join [The F# Open Source Group](http://fsharp.github.com). We use [github](https://github.com/fsharp/fsharpbinding) for tracking work items and suggestions.
