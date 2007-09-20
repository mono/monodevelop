ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Database
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Components.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Database/MonoDevelop.Database.Sql.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data \
	-r:System.Xml

FILES =  \
	Codons/DataGridContentRendererCodon.cs \
	Commands/DataGridCommands.cs \
	Commands/SqlEditorCommands.cs \
	Containers/ColumnContainer.cs \
	Containers/ConditionContainer.cs \
	Containers/ParameterContainer.cs \
	Containers/SchemaContainers.cs \
	Dialogs/DatabaseConnectionSettingsDialog.cs \
	Dialogs/SelectColumnDialog.cs \
	Dialogs/ShowImageDialog.cs \
	Dialogs/ShowTextDialog.cs \
	Dialogs/WaitDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Database.Components.DatabaseConnectionSettingsDialog.cs \
	gtk-gui/MonoDevelop.Database.Components.DataGrid.cs \
	gtk-gui/MonoDevelop.Database.Components.SelectColumnDialog.cs \
	gtk-gui/MonoDevelop.Database.Components.ShowImageDialog.cs \
	gtk-gui/MonoDevelop.Database.Components.ShowTextDialog.cs \
	gtk-gui/MonoDevelop.Database.Components.SqlEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Components.WaitDialog.cs \
	Widgets/CellRendererTypeCombo.cs \
	Widgets/ColumnMappingWidget.cs \
	Widgets/DatabaseConnectionContextComboBox.cs \
	Widgets/DataGrid/DataGrid.cs \
	Widgets/DataGrid/DataGridColumn.cs \
	Widgets/DataGrid/Renderers/BlobContentRenderer.cs \
	Widgets/DataGrid/Renderers/BooleanContentRenderer.cs \
	Widgets/DataGrid/Renderers/ByteContentRenderer.cs \
	Widgets/DataGrid/Renderers/DecimalContentRenderer.cs \
	Widgets/DataGrid/Renderers/DoubleContentRenderer.cs \
	Widgets/DataGrid/Renderers/FloatContentRenderer.cs \
	Widgets/DataGrid/Renderers/IDataGridContentRenderer.cs \
	Widgets/DataGrid/Renderers/IntegerContentRenderer.cs \
	Widgets/DataGrid/Renderers/LongContentRenderer.cs \
	Widgets/DataGrid/Renderers/NullContentRenderer.cs \
	Widgets/DataGrid/Renderers/ObjectContentRenderer.cs \
	Widgets/DataGrid/Renderers/StringContentRenderer.cs \
	Widgets/DataGrid/Visualizers/AbstractDataGridVisualizer.cs \
	Widgets/DataGrid/Visualizers/DataGridItemCommandHandler.cs \
	Widgets/DataGrid/Visualizers/IDataGridVisualizer.cs \
	Widgets/DataGrid/Visualizers/ImageVisualizer.cs \
	Widgets/DataGrid/Visualizers/TextVisualizer.cs \
	Widgets/DataGrid/Visualizers/XmlTextVisualizer.cs \
	Widgets/DataGrid/Visualizers/XmlTreeVisualizer.cs \
	Widgets/ProjectDirectoryComboBox.cs \
	Widgets/SchemaMappingWidget.cs \
	Widgets/SelectColumnWidget.cs \
	Widgets/SelectSchemaWidget.cs \
	Widgets/SortedColumnListStore.cs \
	Widgets/SqlEditorWidget.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.Database.Components.addin.xml

ADDIN = MonoDevelop.Database.Components.addin.xml

if ENABLE_DATABASE
all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


if ENABLE_DATABASE
assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ADDIN)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST =

include $(top_srcdir)/Makefile.include
