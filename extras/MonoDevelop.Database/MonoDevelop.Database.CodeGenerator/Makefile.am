ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.CodeGenerator.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../build/MonoDevelop.Database.Components.dll \
	-r:../build/MonoDevelop.Database.Designer.dll \
	-r:../build/MonoDevelop.Database.Query.dll \
	-r:../build/MonoDevelop.Database.Sql.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data

FILES =  \
	Commands/ToolCommands.cs \
	Dialogs/GenerateDataClassesDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Database.CodeGenerator.GenerateDataClassesDialog.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.Database.CodeGenerator.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
