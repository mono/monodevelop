
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Refactoring
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Refactoring.dll

DEPS =  \
	$(top_builddir)/build/AddIns/VersionControl/Diff.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Refactoring.RefactoringPreviewDialog.cs \
	gtk-gui/MonoDevelop.Refactoring.RenameItemDialog.cs \
	MonoDevelop.Refactoring/RefactoringPreviewDialog.cs \
	MonoDevelop.Refactoring/RefactoryCommands.cs \
	MonoDevelop.Refactoring/Rename.cs \
	MonoDevelop.Refactoring/RenameItemDialog.cs 

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Refactoring.addin.xml 

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Refactoring
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
