# F# Language Support for MonoDevelop / Xamarin Studio

This project contains advanced editing support for the F# addin in MonoDevelop, Xamarin Studio and Visual Studio for Mac.

##Features
* Code completion
* Syntax highlighting
* Tooltips
* Debugging 
* F# Interactive scripting (Alt-Enter execution)
* Templates (Console Application, Library, Tutorial Project, Gtk Project, iOS, Android)
* more...


### Prerequisites

To use F# language support please ensure that you have F# installed on your system, for details on this please see http://fsharp.org

### Installation

This addin is included by default for MonoDevelop/Xamarin Studio/Visual Studio for Mac.

### Building and installing from scratch

This code is directly part of the `monodevelop` repository so the easiest ways of building is to clone monodevelop and work in the submodule directly:

```bash
git clone git@github.com:mono/monodevelop --recursive
cd monodevelop
./configure
make
```


### Can't get it to work?  

Don't give up! Add an issue to this repository. Your issue will be seen by the developers.


### Notes on Manual Testing (old instructions, unverified)

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
  - Run msbuild on a few .fsproj (this is nothing to do with the binding, it is just fsharp/fsharp)

### Debugging Tips (old instructions, unverified)

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

### Building the addin separately (old instructions, unverified)

To configure and compile the addin seperately then the following commands can be executed from the addin directory (/main/external/fsharpbining if cloning as part of monodevelop)

```bash
./configure.sh 
make 
make install
```

If MonoDevelop is installed in an unusual prefix you will need to invoke `configure.sh` with e.g. `--prefix=/path/to/prefix/lib/monodevelop`. Use `./configure.sh --help` to see a list of the paths searched by default.

If you subsequently make changes to the add-in, you will need to `make install` again and restart MonoDevelop/Xamarin Studio. 

The first time you `make install` the AddIn you'll override Xamarin's official one and it will initially be set to disabled, so you need to go to the AddIn manager and ensure the F# AddIn is enabled.  

**Note:**  One thing to check is the the version specified in `configure.fsx` is higher than the pre-installed version, if it's not then the local addin will not be loaded.   

For reference on Mac the locally installed addin is at the following location:  ```/Users/<username>/Library/Application Support/XamarinStudio-6.0/LocalInstall/AddIns/fsharpbinding/<version>```

### Build on Windows (old instructions, unverified, builds and installs the Debug version into Xamarin Studio - adjust as needed)

```dos
configure.bat
build-and-install-debug.bat
```

If you subsequently make changes to the add-in, you will need to `build-and-install-debug.bat` again and restart MonoDevelop/Xamarin Studio. 

For more information about F# see [The F# Software Foundation](http://fsharp.org). Join [The F# Core Engineering Group](http://fsharp.github.io). 
