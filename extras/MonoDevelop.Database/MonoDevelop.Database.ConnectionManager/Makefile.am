ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.ConnectionManager.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../build/MonoDevelop.Database.Components.dll \
	-r:../build/MonoDevelop.Database.Designer.dll \
	-r:../build/MonoDevelop.Database.Query.dll \
	-r:../build/MonoDevelop.Database.Sql.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data \
	-r:System.Xml

FILES =  \
	Commands/ConnectionManagerCommands.cs \
	Commands/ToolbarCommands.cs \
	ConnectionManagerPad.cs \
	FakeNodes.cs \
	gtk-gui/generated.cs \
	NodeBuilders/AggregatesNodeBuilder.cs \
	NodeBuilders/ColumnNodeBuilder.cs \
	NodeBuilders/ColumnsNodeBuilder.cs \
	NodeBuilders/ConnectionContextCollectionNodeBuilder.cs \
	NodeBuilders/ConnectionContextNodeBuilder.cs \
	NodeBuilders/ConstraintNodeBuilder.cs \
	NodeBuilders/ConstraintsNodeBuilder.cs \
	NodeBuilders/GroupsNodeBuilder.cs \
	NodeBuilders/LanguagesNodeBuilder.cs \
	NodeBuilders/OperatorsNodeBuilder.cs \
	NodeBuilders/ParameterNodeBuilder.cs \
	NodeBuilders/ParametersNodeBuilder.cs \
	NodeBuilders/ProcedureNodeBuilder.cs \
	NodeBuilders/ProceduresNodeBuilder.cs \
	NodeBuilders/RolesNodeBuilder.cs \
	NodeBuilders/RulesNodeBuilder.cs \
	NodeBuilders/SequencesNodeBuilder.cs \
	NodeBuilders/TableNodeBuilder.cs \
	NodeBuilders/TablesNodeBuilder.cs \
	NodeBuilders/TriggersNodeBuilder.cs \
	NodeBuilders/TypesNodeBuilder.cs \
	NodeBuilders/UserNodeBuilder.cs \
	NodeBuilders/UsersNodeBuilder.cs \
	NodeBuilders/ViewNodeBuilder.cs \
	NodeBuilders/ViewsNodeBuilder.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Database.ConnectionManager.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
