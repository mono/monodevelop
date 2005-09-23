FILES = \
Commands/Commands.cs \
Gui/NodeBuilders/DatabaseNodeBuilder.cs \
Gui/NodeBuilders/UsersNodeBuilder.cs \
Gui/NodeBuilders/UserNodeBuilder.cs \
Gui/NodeBuilders/LanguagesNodeBuilder.cs \
Gui/NodeBuilders/TypesNodeBuilder.cs \
Gui/NodeBuilders/ProviderCollectionNodeBuilder.cs \
Gui/NodeBuilders/ConstraintsNodeBuilder.cs \
Gui/NodeBuilders/AggregatesNodeBuilder.cs \
Gui/NodeBuilders/TablesNodeBuilder.cs \
Gui/NodeBuilders/ViewNodeBuilder.cs \
Gui/NodeBuilders/ProceduresNodeBuilder.cs \
Gui/NodeBuilders/ProcedureNodeBuilder.cs \
Gui/NodeBuilders/OperatorsNodeBuilder.cs \
Gui/NodeBuilders/GroupsNodeBuilder.cs \
Gui/NodeBuilders/ColumnsNodeBuilder.cs \
Gui/NodeBuilders/RolesNodeBuilder.cs \
Gui/NodeBuilders/ConstraintNodeBuilder.cs \
Gui/NodeBuilders/TriggersNodeBuilder.cs \
Gui/NodeBuilders/TableNodeBuilder.cs \
Gui/NodeBuilders/RulesNodeBuilder.cs \
Gui/NodeBuilders/SequencesNodeBuilder.cs \
Gui/NodeBuilders/ColumnNodeBuilder.cs \
Gui/NodeBuilders/ViewsNodeBuilder.cs \
Gui/NodeBuilders/ParametersNodeBuilder.cs \
Gui/SqlDefinitionPad.cs \
Gui/FakeNodes.cs \
Gui/DatabasePad.cs \
Gui/ConnectionDialog.cs \
Gui/DataGridView.cs \
Gui/SqlQueryView.cs \
Gui/DbProviderCollection.cs \
MonoQueryService.cs \
AssemblyInfo.cs

RES = \
monoquery.glade \
icons/MonoQuery.Addconnection \
icons/MonoQuery.Column \
icons/MonoQuery.ColumnFK \
icons/MonoQuery.ColumnPK \
icons/MonoQuery.Connect \
icons/MonoQuery.Database \
icons/MonoQuery.DateTime \
icons/MonoQuery.Disconnect \
icons/MonoQuery.Procedure \
icons/MonoQuery.Table \
icons/MonoQuery.Tables \
icons/MonoQuery.User \
icons/MonoQuery.View \
icons/MonoQuery.Stop \
icons/MonoQuery.RunFromCursor \
icons/MonoQuery.Explain \
icons/MonoQuery.Param \
icons/MonoQuery.Numeric \
icons/MonoQuery.Execute \
icons/MonoQuery.Database.Connected \
icons/MonoQuery.Database.Disconnected \
icons/MonoQuery.String \
icons/MonoQuery.Providers \
icons/MonoQuery.Blob \
icons/MonoQuery.SqlQueryView


build_sources = $(addprefix $(srcdir)/, $(FILES))

DLL = $(ADDIN_BUILD)/MonoQuery.dll
ADDIN = MonoQuery.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoQuery

REFS = /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
       /r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
       /r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
       /r:$(top_builddir)/build/AddIns/MonoQuery/Mono.Data.Sql.dll \
       /r:System.Data.dll \
       $(GTK_SHARP_LIBS) \
       $(GLADE_SHARP_LIBS) \
       $(GTKSOURCEVIEW_SHARP_LIBS)

all: $(ADDIN_BUILD)/$(ADDIN) $(DLL)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(DLL): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS) \
	$(RES:%=/resource:%)

CLEANFILES = $(DLL) $(DLL).mdb $(ADDIN_BUILD)/$(ADDIN)

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

monoquerydir = $(MD_ADDIN_DIR)/MonoQuery
monoquery_DATA = $(DLL) $(ADDIN_BUILD)/$(ADDIN)

