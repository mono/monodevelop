## F# Language Support for MonoDevelop and Xamarin Studio


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

Requires MonoDevelop or Xamarin Studio 4.0.13 and later versions

### Prerequisites

To use F# language support please ensure that you have F# installed on your system, for details on this please see http://fsharp.org

### Installation

First check install MonoDevelop/Xamarin Studio. Check if F# support is already installed using the AddIn manager.
   MonoDevelop/Xamarin Studio
        --> Add-in manager 
        --> Language Bindings 
		--> Check for F# binding

If so, just use it, no installation is required.

If not, install the F# Language Binding via the AddIn manager.

   MonoDevelop/Xamarin Studio
        --> Add-in manager 
        --> Gallery
        --> Language Bindings 
        --> F# Language Binding


### Building and installing from scratch

Normally you should get the binding from the repository. If you want to build and install it yourself and develop it, try this:


### Build on Mac/Linux:

First get nuget.exe and install the required nuget packages:

Now make:

```bash
cd monodevelop
./configure.sh 
make 
make install
```

If Monodevelop is installed in an unusual prefix you will need to invoke `configure.sh` with e.g. `--prefix=/path/to/prefix/lib/monodevelop`. Use `./configure.sh --help` to see a list of the paths searched by default.

If you subsequently make changes to the add-in, you will need to `make install` again and restart MonoDevelop/Xamarin Studio. 

The first time you `make install` the AddIn you'll override Xamarin's official one and it will initially be set to disabled, so you need to go to the AddIn manager and ensure the F# AddIn is enabled.  

**Note:**  One thing to check is the the version specified in `configure.fsx` is higher than the pre-installed version, if it's not then the local addin will not be loaded.   

For reference on Mac the addin is installed locally at the following location:  ```/Users/<username>/Library/Application Support/XamarinStudio-5.0/LocalInstall/Addins/fsharpbinding/<version>```

### Build on Windows (builds and installs the Debug version into Xamarin Studio - adjust as needed)

```dos
cd monodevelop
configure.bat
build-and-install-debug.bat
```

If you subsequently make changes to the add-in, you will need to `build-and-install-debug.bat` again and restart MonoDevelop/Xamarin Studio. 

### Can't get it to work?  

Don't give up! Add an issue to [the issue tracker](https://github.com/fsharp/fsharpbinding/issues). Your issue will be seen by the developers.

Users of Windows XP wishing to use this project are advised to read the instructions in this [fork](https://github.com/satyagraha/fsharpbinding/tree/windows-xp)

### Notes for Developers

Note: The MonoDevelop/Xamarin Studio developers have now incorporated the binding into all releases 
of MonoDevelop and Xamarin Studio. 

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

On Mac/Linux, please develop using  the 'Makefile' with Mono 3.0 and FSharp 3.1. 

To be able to debug the add-in in Xamarin Studio or Monodevelop, invoke `./configure.sh --debug` or `configure.bat --debug`. This adds the necessary .mdb files to the add-in. 
When configured with `--debug` you can simply `Start debugging` in Xamarin Studio. This will launch a debugged instance of Xamarin Studio. 

On Mac, if you make changes to the add-in after debugging, you will need to restart Xamarin Studio or MonoDevelop before rebuilding. 

Note that you can not build the add-in in release mode when configured with `--debug`. To build a release build, first `./configure.sh` without `--debug`


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


### Notes for People Preparing Releases

Note the MonoDevelop/Xamarin Studio developers have incorporated the binding into all releases 
of MonoDevelop and Xamarin Studio. 

The MonoDevelop Addin mechanism can be hard to easily find information for so here are a couple of links to help get a better understanding.  

  - The addin.xml installation schema description can be found [here](http://addins.monodevelop.com/Source/AddinProjectHelp?projectId=1)
  - Details about publishing an addin can be found [here](http://monodevelop.com/Developers/Articles/Publishing_an_Addin)

The addin used to get released to http://addins.monodevelop.com under project 'FSharp' (project index 48).  This is obsolete due to the addin being packaged as part of the Xamarin Studio release cycle.  Manual updates can be done although this is only really relevant for linux.  Raise an issue for more information or to discuss this.  

To build the .mpack files to upload to this site, use:

	cd monodevelop
	./configure.sh
	make pack

The pack file goes under pack/...

The build is performed against the installed MonoDevelop or Xamarin Studio on your machine.
