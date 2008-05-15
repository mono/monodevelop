
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/NemerleBinding.dll

REFS = -r:System \
       -r:System.Xml \
	-r:System.Drawing \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(NEMERLE_LIBS)

FILES = \
Gui/CodeGenerationPanel.cs \
Project/NemerleParameters.cs \
NemerleBindingCompilerServices.cs \
NemerleLanguageBinding.cs \
NemerleAmbience.cs \
Parser/SharpDevelopTree/Event.cs \
Parser/SharpDevelopTree/Indexer.cs \
Parser/SharpDevelopTree/Constructor.cs \
Parser/SharpDevelopTree/Method.cs \
Parser/SharpDevelopTree/Class.cs \
Parser/SharpDevelopTree/Parameter.cs \
Parser/SharpDevelopTree/Property.cs \
Parser/SharpDevelopTree/Field.cs \
Parser/SharpDevelopTree/Local.cs \
Parser/SharpDevelopTree/ReturnType.cs \
Parser/SharpDevelopTree/Modifier.cs \
Parser/Parser.cs \
Parser/ExpressionFinder.cs \
Parser/CodeGeneration.cs

RES = \
NemerleBinding.addin.xml \
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

if ENABLE_NEMERLE
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(RES:%=/resource:%) \
		$(build_sources) -out:$@ -target:library

if ENABLE_NEMERLE
assemblydir = $(MD_ADDIN_DIR)/BackendBindings/
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)
include $(top_srcdir)/Makefile.include

