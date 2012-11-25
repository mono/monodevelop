# F# Language Support for Open Editors

This project contains advanced editing support for F# for a number of open editors
* MonoDevelop
* Emacs (in progress)
* Vim (in progress)

Join [The F# Open Source Group](http://fsharp.github.com). We use [github](http://fsharp.github.com/fsharpbinding) for tracking work items and suggestions.


## Basic Components

The core component is the FSharp.CompilerBinding.dll. This is used by both fsautocomplete.exe, a command-line utility to sit behind Emacs, Vim and other editing environments, an the MonoDevelop components.

### Basic Components - Building

	./configure
	make

This produces bin/FSharp.CompilerBinding.dll and bin/fsautocomplete.exe.

To understand how to use these components, see the other projects


## MonoDevelop support

Adds open source F# support to the open source editor MonoDevelop.

Features:
* Code completion
* Syntax highlighting
* Tooltips
* Debugging 
* Target .NET 3.5, 4.0, 4.5
* F# Interactive scripting (Alt-Enter execution)
* Templates (Console Application, Library, Tutorial Project, Gtk Project, ASP.NET MVC 4)
* Makefile support
* MonoDevelop includes C# 5.0, ASP.NET and other features
* Supports F# 3.0 type providers (requires F# 3.0)
* xbuild support for Visual Studio .fsproj and .sln files without change (requires Mono 3.0 and F# 3.0)

Requires MonoDevelop 3.0 and later versions


### Installation

On Mac and Linux, first build or install [the F# 3.0 compiler (open source edition)](http://fsharp.github.com/fsharp) if 'fsharpc' is not on your command line. F# 2.0 can also be used.

Then install the F# Language Binding via the MonoDeveop Add-in manager.

   MonoDevelop 
        --> Add-in manager --> Gallery
        --> Language Bindings --> F# Language Binding

Then do this:

   MonoDevelop 
      --> Options/Preferences --> Source Code --> Code Formatting --> Text File --> Convert tabs to spaces


For more information about F# and code examples see these links:
* http://fsharp.net
* http://tryfsharp.org
* http://fssnip.net

### Using the ASP.NET MVC 4 Template

To use the ASP.NET MVC 4 template, a little bit of additional setup is required. The steps are simple and are only required once.

On the Mac, you can clone this repository and do the following:

	cd monodevelop
	./configure.sh
	make add-libraries
  
This adds all of the ASP.NET MVC 4 libraries to the MonoDevelop Addins directory. A few of these libraries have already been added to 
Mono 3.0. If a future release of Mono includes the additional libraries, these manual process can be eliminated.

On Windows, you need to install ASP.NET MVC 4 from [here](http://www.microsoft.com/en-us/download/details.aspx?id=30683). 
You can then create a project from the template, build it, and run. 

If, for whatever reason, the references are not found when you create a project from the template, you can clone this repo
and copy the monodevelop/dependencies/AspNetMvc4 directory to your local project and re-add the references.   


### Building and installing from scratch

Normally you should get the binding from the repository. If you want to build and install it yourself and develop it, try this:

	cd monodevelop
	./configure.sh
	make 
	make install

### Can't get it to work?  

Don't give up! Add an issue to [the issue tracker](http://fsharp.github.com/fsharpbinding/issues). You issue will be seen by the developers.

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

