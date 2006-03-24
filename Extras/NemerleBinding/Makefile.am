
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/NemerleBinding.dll

REFS = -r:System \
       -r:System.Xml \
	-r:System.Drawing \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
Gui/CodeGenerationPanel.cs \
Project/NemerleParameters.cs \
NemerleBindingCompilerServices.cs \
NemerleLanguageBinding.cs 

RES = \
Nemerle.glade \
NemerleEmptyProject.xpt.xml \
NemerleGnomeSharpProject.xpt.xml \
NemerleGtkSharpProject.xpt.xml \
NemerleGtkSharpWindow.xft.xml \
EmptyNemerleFile.xft.xml \
icons/Nemerle.File.EmptyFile \
icons/Nemerle.File.Form \
icons/Nemerle.FileIcon \
icons/Nemerle.Project.EmptyProject \
icons/Nemerle.Project.Form \
icons/Nemerle.ProjectIcon

ADDIN = NemerleBinding.addin.xml

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(RES:%=/resource:%) \
		$(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/BackendBindings/
assembly_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)
include $(top_srcdir)/Makefile.include

