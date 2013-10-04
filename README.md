# F# Language Support for Open Editors

This project contains advanced editing support for F# for a number of open editors
* [MonoDevelop](#monodevelop-support)
* [Emacs](emacs/README.md)
* Vim (in progress)

For more information about F# see [The F# Software Foundation](http://fsharp.org). Join [The F# Open Source Group](http://fsharp.github.com). We use [github](https://github.com/fsharp/fsharpbinding) for tracking work items and suggestions.

## Basic Components

The core component is the FSharp.CompilerBinding.dll. This is used by both fsautocomplete.exe, a command-line utility to sit behind Emacs, Vim and other editing environments, an the MonoDevelop components.

### Basic Components - Building

	./configure.sh
	make

This produces bin/FSharp.CompilerBinding.dll and bin/fsautocomplete.exe. To understand how to use these components, see the other projects.

## MonoDevelop and Xamarin Studio support

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

Requires MonoDevelop or Xamarin Studio 4.0.12 and later versions

### Installation

First check install MonoDevelop/Xamarin Studio. Check if F# support is already installed using the AddIn manager.
   MonoDevelop/Xxamarin Studio 
        --> Add-in manager 
        --> Language Bindings 
		--> Check for F# binding

If so, just use it, no installation is required.

If not, install the F# Language Binding via the AddIn manager.

   MonoDevelop/Xxamarin Studio 
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


#### Build on Linux/Mac:

First get nuget.exe and install the required nuget packages:


Now make:

	cd monodevelop
	./configure.sh 
	make 
	make install

#### Build on Windows (builds and installs the Debug version into Xamarin Studio - adjust as needed)

	cd monodevelop
	configure.bat
	.\install-debug-xamarin-studio.bat



### Can't get it to work?  

Don't give up! Add an issue to [the issue tracker](https://github.com/fsharp/fsharpbinding/issues). You issue will be seen by the developers.

Users of Windows XP wishing to use this project are advised to read the instruction in this [fork](https://github.com/satyagraha/fsharpbinding/tree/windows-xp)

### Notes for Developers

Note (as of 13/9/2013): the MonoDevelop/Xamarin Studio developers are preparing to incorporate the binding into all releases 
of MonoDevelop and Xamarin Studio. Some of the information below will be obsolete once this is done.

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

On Windows, the configuration creates the file `MonoDevelop.FSharpBinding\MonoDevelop.FSharp.windows.fsproj`. 
Be aware that this is not the original file, which is `MonoDevelop.FSharp.fsproj.orig`. The windows file is 
created automatically by the configuration script (`configure.bat`)

On Mac/Linux, please develop using  the 'Makefile' with Mono 3.0 and FSharp 3.0. 

On Mac/Linux, if you make changes to the binding, then loss of completion lists etc. can be disturbing and hard to debug. There are some debugging techniques. To launch MonoDevelop you can use the command:  
```
/Applications/MonoDevelop.app/Contents/MacOS/MonoDevelop --new-window --no-redirect
```
or this command for Xamarin Studio:  
```
"/Applications/Xamarin Studio.app/Contents/MacOS/XamarinStudio" --new-window --no-redirect
```
to enable some logging you can use

	export FSHARPBINDING_LOGGING=*

On Windows you can generally use Visual Studio to help develop the binding. 
You can start Xamarin Studio or MonoDevelop under the debugger using the normal technique:

	devenv /debugexe "c:\Program Files (x86)\Xamarin Studio\bin\XamarinStudio.exe"


## Notes for People Preparing Releases

Note (as of 13/9/2013): the MonoDevelop/Xamarin Studio developers are preparing to incorporate the binding into all releases 
of MonoDevelop and Xamarin Studio. The information below will be obsolete once this is done.

The MonoDevelop Addin mechanism can be hard to easily find information for so here are a couple of links to help get a better understanding.  

  - The addin.xml installation schema description can be found [here](http://addins.monodevelop.com/Source/AddinProjectHelp?projectId=1)
  - Details about publishing an addin can be found [here](http://monodevelop.com/Developers/Articles/Publishing_an_Addin)

The addin gets released to http://addins.monodevelop.com under project 'FSharp' (project index 48). Contact @7sharp9, @tpetricek or @funnelweb to make an update.

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

The build is performed against the MonoDevelop binaries we depend on in dependencies/... which have been copied from MonoDevelop installs.
