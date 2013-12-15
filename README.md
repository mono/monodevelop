# F# Language Support for Open Editors

This project contains advanced editing support for F# for a number of open editors. It is made up of the following projects:
* [F# mode for Emacs](emacs/README.md)
* [F# addin for MonoDevelop and Xamarin Studio](monodevelop/README.md)
* Some reusable components shared by these (see below)

See the [F# Cross-Platform Development Guide](http://fsharp.org/guides/mac-linux-cross-platform/index.html#editing) for F# with Sublime Text 2, Vim and other editors not covered here.

## Building and Using

See  [emacs/README.md](emacs/README.md) or  [monodevelop/README.md](monodevelop/README.md)


## Shared Components

The core shared component is FSharp.Compiler.Editor.dll from the 
community [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service) project. 
This is used by both [fsautocomplete.exe](https://github.com/fsharp/fsharpbinding/tree/master/FSharp.AutoComplete), 
a command-line utility to sit behind Emacs, Vim and other editing environments components. 

Building:

	./configure.sh
	make

This produces bin/fsautocomplete.exe. To understand how to use these components, see the other projects.

An old component called FSharp.CompilerBinding.dll is also present, it was used as a shim to the F# compiler before
the availability of FSharp.Compiler.Editor.dll.


For more information about F# see [The F# Software Foundation](http://fsharp.org). Join [The F# Open Source Group](http://fsharp.github.com). We use [github](https://github.com/fsharp/fsharpbinding) for tracking work items and suggestions.
