
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/ILAsmBinding.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:System \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	Gui/CompilerParametersPanel.cs \
	ILAsmCompilerManager.cs \
	ILAsmLanguageBinding.cs \
	Project/ILAsmCompilerParameters.cs

RES =  \
	icons/ILAsm.Project.DOSProject \
	ILAsmConsoleProject.xpt.xml


ADDIN = ILAsmBinding.addin.xml

assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY) $(ADDIN)

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(build_resources:%=/resource:%) $(build_sources) $(REFS) -out:$@ -target:library

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)
include $(top_srcdir)/Makefile.include

