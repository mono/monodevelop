ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Components.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../build/MonoDevelop.Database.Sql.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data \
	-r:System.Xml

FILES =  \
	Codons/DataGridContentRendererCodon.cs \
	Codons/DataGridVisualizerCodon.cs \
	Commands/AbstractDatabaseProviderList.cs \
	Commands/DataGridCommands.cs \
	Commands/SqlEditorCommands.cs \
	ComponentHelper.cs \
	Containers/ColumnContainer.cs \
	Containers/ConditionContainer.cs \
	Containers/ParameterContainer.cs \
	Containers/SchemaContainers.cs \
	Containers/TableContainer.cs \
	Dialogs/DatabaseConnectionSettingsDialog.cs \
	Dialogs/SelectColumnDialog.cs \
	Dialogs/ShowImageDialog.cs \
	Dialogs/ShowTextDialog.cs \
	Dialogs/WaitDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Database.Components.ConnectionSettingsWidget.cs \
	gtk-gui/MonoDevelop.Database.Components.DatabaseConnectionSettingsDialog.cs \
	gtk-gui/MonoDevelop.Database.Components.DataGrid.cs \
	gtk-gui/MonoDevelop.Database.Components.SelectColumnDialog.cs \
	gtk-gui/MonoDevelop.Database.Components.ShowImageDialog.cs \
	gtk-gui/MonoDevelop.Database.Components.ShowTextDialog.cs \
	gtk-gui/MonoDevelop.Database.Components.SqlEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Components.WaitDialog.cs \
	Widgets/CellRendererTypeCombo.cs \
	Widgets/ColumnMappingWidget.cs \
	Widgets/ConnectionSettingsWidget.cs \
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
	Widgets/DataGrid/Views/ImageVisualizerView.cs \
	Widgets/DataGrid/Views/TextVisualizerView.cs \
	Widgets/DataGrid/Visualizers/DefaultVisualizers.cs \
	Widgets/DataGrid/Visualizers/IDataGridVisualizer.cs \
	Widgets/ProjectDirectoryComboBox.cs \
	Widgets/SelectColumnWidget.cs \
	Widgets/SelectSchemaWidget.cs \
	Widgets/SelectTableWidget.cs \
	Widgets/SortedColumnListStore.cs \
	Widgets/SortedTableListStore.cs \
	Widgets/SqlEditorWidget.cs \
	Widgets/TableMappingWidget.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.Database.Components.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
