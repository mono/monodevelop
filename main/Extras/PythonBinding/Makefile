
CSC = mcs /debug
ASSEMBLY = PythonBinding.dll
ADDIN = PythonBinding.addin.xml

TEMPLATE1 = templates/PythonConsoleProject.xpt.xml
TEMPLATE2 = templates/PythonGtkProject.xpt.xml

DLLS = /r:../../Core/build/bin/MonoDevelop.Core.dll \
	/r:../../Core/build/bin/MonoDevelop.Base.dll \
	/r:../../Core/build/bin/MonoDevelop.SourceEditor.dll \
	/r:../../Core/build/bin/MonoDevelop.Gui.Widgets.dll \
	-pkg:gtk-sharp-2.0

#	$(BASE_DEPENDENCIES_LIBS)

FILES = \
./Gui/CompilerParametersPanel.cs \
./Project/CompilationTarget.cs \
./Project/PythonProject.cs \
./Project/PythonCompilerParameters.cs \
./PythonCompilerManager.cs \
./AssemblyInfo.cs \
./PythonLanguageBinding.cs \
./PythonExecutionManager.cs

#build_sources = $(addprefix $(srcdir)/, $(FILES))

#assemblydir = $(libdir)/monodevelop/AddIns/AddIns/BackendBindings
#assembly_DATA = $(ASSEMBLY)

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES)
	$(CSC) $(FILES) $(DLLS) /out:$(ASSEMBLY) /target:library \
	&& cp $(ASSEMBLY) ../../Core/build/AddIns/AddIns/BackendBindings/. \
	&& cp $(ADDIN) ../../Core/build/AddIns/AddIns/BackendBindings/. \
	&& cp $(TEMPLATE1) ../../Core/build/AddIns/AddIns/BackendBindings/templates/. \
	&& cp $(TEMPLATE2) ../../Core/build/AddIns/AddIns/BackendBindings/templates/.

#CLEANFILES = $(ASSEMBLY)
#EXTRA_DIST = $(FILES)

clean:
	rm -f $(ASSEMBLY)
