# Makefile for compiling and installing F# MonoDevelop plugin on Mono
#   run 'make' to compile the plugin (dll + debug info)
#   run 'make install' to copy the compiled plugin to MonoDevelop folders
#   run 'make package' to create a deployment binary package with addin (for repository)

# Here are a few paths that need to be configured first:
MDROOT  = ../../monodevelop-2.4/monodevelop/main/build
MONOBIN = /usr/lib/mono
FSBIN = ../../../fsharp/bin
MDRUN = mono $(MDROOT)/bin/mdrun.exe

# Settings for Windows
MDRUN = $(MDROOT)/bin/mdrun

# Settings for MAC
# MDROOT = /Applications/MonoDevelop.app/Contents/MacOS/lib/monodevelop
# MONOBIN = /Library/Frameworks/Mono.framework/Versions/2.8/lib/mono
# FSBIN = /usr/lib/fsharp/
# MDRUN = mono $(MDROOT)/bin/mdrun.exe

# The following should be the default configuration (no need to change these)
FSC = fsharpc
CSC = gmcs
MDBIN = $(MDROOT)/bin

# Resources and files to be compiled/included as part of the project
RESOURCES = \
	--resource:FSharp.MonoDevelop/Resources/FSharpBinding.addin.xml \
	--resource:FSharp.MonoDevelop/Resources/EmptyFSharpSource.xft.xml \
	--resource:FSharp.MonoDevelop/Resources/EmptyFSharpScript.xft.xml \
	--resource:FSharp.MonoDevelop/Resources/FSharpConsoleProject.xpt.xml \
	--resource:FSharp.MonoDevelop/Resources/fsharp-icon-32.png \
	--resource:FSharp.MonoDevelop/Resources/fsharp-script-32.png \
	--resource:FSharp.MonoDevelop/Resources/fsharp-file-icon.png \
	--resource:FSharp.MonoDevelop/Resources/fsharp-project-icon.png \
	--resource:FSharp.MonoDevelop/Resources/fsharp-script-icon.png \
	--resource:FSharp.MonoDevelop/Resources/FSharpSyntaxMode.xml

FILES = \
	FSharp.MonoDevelop/PowerPack/CodeDomVisitor.fs \
	FSharp.MonoDevelop/PowerPack/CodeDomGenerator.fs \
	FSharp.MonoDevelop/PowerPack/CodeProvider.fs \
	FSharp.MonoDevelop/PowerPack/LazyList.fsi \
	FSharp.MonoDevelop/PowerPack/LazyList.fs \
	FSharp.MonoDevelop/Services/Mailbox.fs \
	FSharp.MonoDevelop/Services/Parameters.fs \
	FSharp.MonoDevelop/Services/FSharpCompiler.fs \
	FSharp.MonoDevelop/Services/CompilerLocationUtils.fs \
	FSharp.MonoDevelop/Services/Common.fs \
	FSharp.MonoDevelop/Services/Parser.fs \
	FSharp.MonoDevelop/Services/LanguageService.fs \
	FSharp.MonoDevelop/Services/CompilerService.fs \
	FSharp.MonoDevelop/Services/InteractiveSession.fs \
	FSharp.MonoDevelop/FSharpInteractivePad.fs \
	FSharp.MonoDevelop/FSharpOptionsPanels.fs \
	FSharp.MonoDevelop/FSharpSyntaxMode.fs \
	FSharp.MonoDevelop/FSharpResourceIdBuilder.fs \
	FSharp.MonoDevelop/FSharpLanguageBinding.fs \
	FSharp.MonoDevelop/FSharpParser.fs \
	FSharp.MonoDevelop/FSharpTextEditorCompletion.fs \
	FSharp.MonoDevelop/FSharpResolverProvider.fs

REFERENCES = \
	-r:$(MONOBIN)/2.0/mscorlib.dll \
	-r:System.dll -r:System.Xml.dll \
	-r:$(MDBIN)/MonoDevelop.Core.dll \
	-r:$(MDBIN)/MonoDevelop.Ide.dll \
	-r:$(MDBIN)/Mono.TextEditor.dll \
	-r:$(FSBIN)/FSharp.Core.dll \
	-r:$(FSBIN)/FSharp.Compiler.dll \
	-r:$(FSBIN)/FSharp.Compiler.Interactive.Settings.dll \
	-r:$(FSBIN)/FSharp.Compiler.Server.Shared.dll \
	-r:$(MONOBIN)/gtk-sharp-2.0/atk-sharp.dll \
	-r:$(MONOBIN)/gtk-sharp-2.0/pango-sharp.dll \
	-r:$(MONOBIN)/gtk-sharp-2.0/gtk-sharp.dll \
	-r:$(MONOBIN)/gtk-sharp-2.0/gdk-sharp.dll \
	-r:$(MONOBIN)/gtk-sharp-2.0/glib-sharp.dll

OPTIONS = \
	--noframework --debug --optimize- --target:library -r:FSharp.MonoDevelop/bin/FSharpBinding.Gui.dll --out:FSharp.MonoDevelop/bin/FSharpBinding.dll

# CSharp project that contains designer generated GTK stuff for project options

GUIFILES = \
	FSharp.MonoDevelop/Gui/FSharpBuildOrderWidget.cs \
	FSharp.MonoDevelop/Gui/FSharpSettingsWidget.cs \
	FSharp.MonoDevelop/Gui/FSharpCompilerOptionsWidget.cs \
	FSharp.MonoDevelop/Gui/gtk-gui/FSharp.MonoDevelop.Gui.FSharpBuildOrderWidget.cs \
	FSharp.MonoDevelop/Gui/gtk-gui/FSharp.MonoDevelop.Gui.FSharpSettingsWidget.cs \
	FSharp.MonoDevelop/Gui/gtk-gui/FSharp.MonoDevelop.Gui.FSharpCompilerOptionsWidget.cs \
	FSharp.MonoDevelop/Gui/gtk-gui/generated.cs

GUIOPTIONS = \
	-debug+ -out:FSharp.MonoDevelop/bin/FSharpBinding.Gui.dll -target:library

all: gui
	$(FSC) $(OPTIONS) $(REFERENCES) $(RESOURCES) $(FILES) 

gui:
	$(CSC) $(GUIOPTIONS) $(REFERENCES) $(GUIFILES) 

install:
	cp FSharp.MonoDevelop/bin/FSharpBinding.* $(MDROOT)/AddIns/BackendBindings/

uninstall:
	rm $(MDROOT)/AddIns/BackendBindings/FSharpBinding.*

package:
	mkdir -p FSharp.MonoDevelop/bin/repository
	cp FSharp.MonoDevelop/bin/FSharpBinding.* FSharp.MonoDevelop/bin/repository
	cp FSharp.MonoDevelop/Resources/FSharpBinding.addin.xml FSharp.MonoDevelop/bin/repository
	$(MDRUN) setup pack FSharp.MonoDevelop/bin/repository/FSharpBinding.addin.xml -d:Repository

