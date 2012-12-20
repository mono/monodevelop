# F# Language Support for Open Editors

This project contains advanced editing support for F# for a number of open editors
* [MonoDevelop](#monodevelop-support)
* [Emacs (in progress)](#emacs-support)
* Vim (in progress)

For more information about F# see [The F# Software Foundation](http://fsharp.org). Join [The F# Open Source Group](http://fsharp.github.com). We use [github](https://github.com/fsharp/fsharpbinding) for tracking work items and suggestions.


## Basic Components

The core component is the FSharp.CompilerBinding.dll. This is used by both fsautocomplete.exe, a command-line utility to sit behind Emacs, Vim and other editing environments, an the MonoDevelop components.

### Basic Components - Building

	./configure.sh
	make

This produces bin/FSharp.CompilerBinding.dll and bin/fsautocomplete.exe. To understand how to use these components, see the other projects.

## MonoDevelop support

Adds open source F# support to the open source editor MonoDevelop. Features:
* Code completion
* Syntax highlighting
* Tooltips
* Debugging 
* Target .NET 3.5, 4.0, 4.5
* F# Interactive scripting (Alt-Enter execution)
* Templates (Console Application, Library, Tutorial Project, Gtk Project, Web Programming)
* Makefile support
* Supports F# 3.0 type providers (requires F# 3.0)
* xbuild support for Visual Studio .fsproj and .sln files without change (requires Mono 3.0 and F# 3.0)
* MonoDevelop also includes C# 5.0 and other features

Requires MonoDevelop 3.0 and later versions

### Installation

[Install F#](http://fsharp.org). Then install the F# Language Binding via the MonoDeveop Add-in manager.

   MonoDevelop 
        --> Add-in manager 
        --> Gallery
        --> Language Bindings 
        --> F# Language Binding

### Using the ASP.NET MVC 4 Template

On Windows, you need to install ASP.NET MVC 4 from [here](http://www.microsoft.com/en-us/download/details.aspx?id=30683). 
You can then create a project from the template, build it, and run. 

On Mac and Linux the template includes a copy of the basic ASP.NET MVC 4 core DLLs.

### Building and installing from scratch

Normally you should get the binding from the repository. If you want to build and install it yourself and develop it, try this:

	cd monodevelop
	./configure.sh
	make 
	make install

### Can't get it to work?  

Don't give up! Add an issue to [the issue tracker](https://github.com/fsharp/fsharpbinding/issues). You issue will be seen by the developers.

### Notes for Developers

To check things are working try a few different things somewhat at random:
  - Check the F# templates are appearing
  - Create a console project (NOTE: retarget it to .NET 4.0 using right-click->options->General)
  - Check there are completion lists in the console project e.g. for 'System.' and 'System.Console.WriteLine(' and 'List.'
  - Check you can build the console project
  - Check you can run the console project
  - Check you can "debug-step-into" the console project
  - Check you can set a break point in the console project
  - Check there are type tips showing when you move the mouse over code identifiers
  - Load an existing .fsproj (e.g. see MonoDevelop.FSharpBinding/tests/projects/...) and check if completion works etc.
  - Run xbuild on a few .fsproj (this is nothing to do with the binding, it is just fsharp/fsharp)

There are a couple of known issues, see https://github.com/fsharp/fsharpbinding/issues.

On windows, use the file MonoDevelop.FSharpBinding\MonoDevelop.FSharp.windows.fsproj. Be aware that this is not the original file, which is MonoDevelop.FSharp.orig.  The windows file is created automatically now and then to help development on Windows.

On Mac/Linux, please develop using  the 'Makefile' with Mono 3.0 and FSharp 3.0. There is an old Makefile for the days before xbuild works, but this is not used to prepare distributions.

If you make changes to the binding, then loss of completion lists etc. can be disturbing and hard to debug. There are some debugging techniques. To launch MD you can use
   /Applications/MonoDevelop.app/Contents/MacOS/MonoDevelop --new-window --no-redirect

To enable some logging you can use
  export FSHARPBINDING_LOGGING=*


## Emacs support

The Emacs integration currently only supports working with a single file

Features:

- Interactive F# buffer
- Indentation
- Syntax highlighter


### Installation

`fsharp-mode` is available on [MELPA](http://melpa.milkbox.net).

To download it,`package.el` is the built-in package manager in Emacs 24+. On Emacs 23
you will need to get [package.el](http://bit.ly/pkg-el23) yourself if you wish to use it.

If you're not already using MELPA, add this to your
`~/.emacs.d/init.el` (or equivalent) and load it with <kbd>M-x eval-buffer</kbd>.

```lisp
(require 'package)
(add-to-list 'package-archives
             '("melpa" . "http://melpa.milkbox.net/packages/") t)
(package-initialize)
```

And then you can install fsharp-mode with the following command:

<kbd>M-x package-install [RET] fsharp-mode [RET]</kbd>

or by adding this bit of Emacs Lisp code to your Emacs initialization file(`.emacs` or `init.el`):

```lisp
(when (not (package-installed-p 'fsharp-mode))
  (package-install 'fsharp-mode))
```

If the installation doesn't work try refreshing the package list:

<kbd>M-x package-refresh-contents [RET]</kbd>

### Manual installation

In order to install the F# mode, you have to tell Emacs where to find it. This is done by
adding some commands to the init file.

Copy the `fsharpbinding/emacs/` directory in your `~/.emacs.d`
directory (or a place of your choice) and rename it to `fsharp`
Assuming you now have a `~/.emacs.d/fsharp` directory, copy the following lines to your
init file (usually `~/.emacs` or `init.el`).

```lisp
(setq load-path (cons "~/.emacs.d/fsharp" load-path))
(setq auto-mode-alist (cons '("\\.fs[iylx]?$" . fsharp-mode) auto-mode-alist))
(autoload 'fsharp-mode "fsharp" "Major mode for editing F# code." t)
(autoload 'run-fsharp "inf-fsharp" "Run an inferior F# process." t)
```

### Configuration

If `fsc` and `fsi` are in your path, that's all you need. Otherwise,
you can add these two following lines to set the path to the compiler
and interactive F#.

On Windows (adapt the path if needed):

```lisp
(setq inferior-fsharp-program "\"c:\\Program Files\\Microsoft F#\\v4.0\\Fsi.exe\"")
(setq fsharp-compiler "\"c:\\Program Files\\Microsoft F#\\v4.0\\Fsc.exe\"")
```

On Unix (adapt the path if needed):

```lisp
(setq inferior-fsharp-program "mono ~/fsi.exe --readline-")
(setq fsharp-compiler "mono ~/fsc.exe")
```
### Bindings

If you are new to Emacs, you might want to use the menu (call
menu-bar-mode if you don't see it). However, it's usually faster to learn
a few useful bindings:

- <kbd>C-c C-r</kbd>:       Evaluate region
- <kbd>C-c C-e</kbd>:       Evaluate current toplevel phrase
- <kbd>C-M-x</kbd>:         Evaluate current toplevel phrase
- <kbd>C-M-h</kbd>:         Mark current toplevel phrase
- <kbd>C-c C-s</kbd>:       Show interactive buffer
- <kbd>C-c C-c</kbd>:       Compile with fsc
- <kbd>C-c x</kbd>:         Run the executable
- <kbd>C-c C-a</kbd>:       Open alternate file (.fsi or .fs)
- <kbd>C-c l</kbd>:         Shift region to left
- <kbd>C-c r</kbd>:         Shift region to right
- <kbd>C-c <up></kbd>:      Move cursor to the beginning of the block

To interrupt the interactive mode, use <kbd>C-c C-c</kbd>. This is useful if your
code does an infinite loop or a very long computation.

If you want to shift the region by 2 spaces, use: <kbd>M-2 C-c r</kbd>

In the interactive buffer, use <kbd>M-RET</kbd> to send the code without
explicitly adding the `;;` thing.


## Notes for People Preparing Releases

The addin gets released to http://addins.monodevelop.com under project 'FSharp' (project index 48). Contact @sega, @tpetricek or @funnelweb to make an update.

To build the .mpack files to upload to this site, use:

	cd monodevelop
	./configure.sh
	make packs

The files go under pack/...

The build process builds several versions of the addin for specific different versions of MonoDevelop.  MonoDevelop APIs can 
change a bit and are not binary compatible. We try to keep up with 
  (a) the latest version available as an Ubuntu package
  (b) the latest version available in the 'Stable' channel on Windows and Mac
  (c) the latest version available in the 'Beta' channel on Windows and Mac

When developing generally use (c)

The build is performed against the MonoDevelop binaries we depend on in dependencies/..., which have been 
snarfed from MonoDevelop installs.
