# F# Language Support for Open Editors

This project contains advanced editing support for F# for a number of open editors
* [MonoDevelop](monodevelop/README.md)
* [Emacs](emacs/README.md)
* Vim (in progress)

Other editors also provide basic editing support for F#. Some links:

* Sublime Text 2
  * [Configuring Sublime Text 2 To Work With F#](http://onor.io/2012/01/26/configuring-sublime-text-2-to-work-with-fsharp/)
  * [Using Sublime Text 2 as F# REPL](http://blog.kulman.sk/using-sublime-text-2-as-f-repl/)

* Vim
  * [Writing and Running F# Scripts with Vim](http://juliankay.com/development/writing-and-running-f-scripts-with-vim/)
  * [Vim Runtime Files for F#](https://github.com/kongo2002/fsharp-vim)

For more information about F# see [The F# Software Foundation](http://fsharp.org). Join [The F# Open Source Group](http://fsharp.github.com). We use [github](https://github.com/fsharp/fsharpbinding) for tracking work items and suggestions.

## Basic Components

The core component is the FSharp.CompilerBinding.dll. This is used by both fsautocomplete.exe, a command-line utility to sit behind Emacs, Vim and other editing environments, an the MonoDevelop components.

Building:

	./configure.sh
	make

This produces bin/FSharp.CompilerBinding.dll and bin/fsautocomplete.exe. To understand how to use these components, see the other projects.

