
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/ILAsmBinding.dll

DLLS = /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.SourceEditor.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
	$(GTK_SHARP_LIBS)

FILES = \
Gui/CompilerParametersPanel.cs \
Project/ILAsmCompilerParameters.cs \
ILAsmCompilerManager.cs \
AssemblyInfo.cs \
ILAsmLanguageBinding.cs

RES = \
ILAsmConsoleProject.xpt.xml \
icons/ILAsm.Project.DOSProject

ADDIN = ILAsmBinding.addin.xml

assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY) $(ADDIN)

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(RES:%=/resource:%) $(build_sources) $(DLLS) -out:$@ -target:library

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)
include $(top_srcdir)/Makefile.include

