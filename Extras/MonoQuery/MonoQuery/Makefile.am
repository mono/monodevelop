

FILES =  \
	AssemblyInfo.cs \
	Commands/Commands.cs \
	Gui/ConnectionDialog.cs \
	Gui/DatabasePad.cs \
	Gui/DataGridView.cs \
	Gui/DbProviderCollection.cs \
	Gui/FakeNodes.cs \
	Gui/NodeBuilders/AggregatesNodeBuilder.cs \
	Gui/NodeBuilders/ColumnNodeBuilder.cs \
	Gui/NodeBuilders/ColumnsNodeBuilder.cs \
	Gui/NodeBuilders/ConstraintNodeBuilder.cs \
	Gui/NodeBuilders/ConstraintsNodeBuilder.cs \
	Gui/NodeBuilders/DatabaseNodeBuilder.cs \
	Gui/NodeBuilders/GroupsNodeBuilder.cs \
	Gui/NodeBuilders/LanguagesNodeBuilder.cs \
	Gui/NodeBuilders/OperatorsNodeBuilder.cs \
	Gui/NodeBuilders/ParametersNodeBuilder.cs \
	Gui/NodeBuilders/ProcedureNodeBuilder.cs \
	Gui/NodeBuilders/ProceduresNodeBuilder.cs \
	Gui/NodeBuilders/ProviderCollectionNodeBuilder.cs \
	Gui/NodeBuilders/RolesNodeBuilder.cs \
	Gui/NodeBuilders/RulesNodeBuilder.cs \
	Gui/NodeBuilders/SequencesNodeBuilder.cs \
	Gui/NodeBuilders/TableNodeBuilder.cs \
	Gui/NodeBuilders/TablesNodeBuilder.cs \
	Gui/NodeBuilders/TriggersNodeBuilder.cs \
	Gui/NodeBuilders/TypesNodeBuilder.cs \
	Gui/NodeBuilders/UserNodeBuilder.cs \
	Gui/NodeBuilders/UsersNodeBuilder.cs \
	Gui/NodeBuilders/ViewNodeBuilder.cs \
	Gui/NodeBuilders/ViewsNodeBuilder.cs \
	Gui/SqlDefinitionPad.cs \
	Gui/SqlQueryView.cs \
	MonoQueryService.cs

RES =  \
	icons/MonoQuery.Addconnection \
	icons/MonoQuery.Blob \
	icons/MonoQuery.Column \
	icons/MonoQuery.ColumnFK \
	icons/MonoQuery.ColumnPK \
	icons/MonoQuery.Connect \
	icons/MonoQuery.Database \
	icons/MonoQuery.Database.Connected \
	icons/MonoQuery.Database.Disconnected \
	icons/MonoQuery.DateTime \
	icons/MonoQuery.Disconnect \
	icons/MonoQuery.Execute \
	icons/MonoQuery.Explain \
	icons/MonoQuery.Numeric \
	icons/MonoQuery.Param \
	icons/MonoQuery.Procedure \
	icons/MonoQuery.Providers \
	icons/MonoQuery.RunFromCursor \
	icons/MonoQuery.SqlQueryView \
	icons/MonoQuery.Stop \
	icons/MonoQuery.String \
	icons/MonoQuery.Table \
	icons/MonoQuery.Tables \
	icons/MonoQuery.User \
	icons/MonoQuery.View \
	monoquery.glade

ASSEMBLY = $(ADDIN_BUILD)/MonoQuery.dll
ADDIN = MonoQuery.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoQuery

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoQuery/Mono.Data.Sql.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:System \
	-r:System.Data \
	-r:System.Xml

if ENABLE_MONOQUERY
all: $(ADDIN_BUILD)/$(ADDIN) $(ASSEMBLY)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS) \
	$(build_resources:%=/resource:%)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN)

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

if ENABLE_MONOQUERY
monoquerydir = $(MD_ADDIN_DIR)/MonoQuery
monoquery_DATA = $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)
endif

include $(top_srcdir)/Makefile.include
