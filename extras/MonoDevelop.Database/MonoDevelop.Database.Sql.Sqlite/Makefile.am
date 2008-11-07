ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Sql.Sqlite.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:$(top_builddir)/build/MonoDevelop.Database.Components.dll \
	-r:$(top_builddir)/build/MonoDevelop.Database.Designer.dll \
	-r:$(top_builddir)/build/MonoDevelop.Database.Sql.dll \
	-r:Mono.Data \
	-r:Mono.Data.Sqlite \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data

FILES =  \
	Dialogs/SqliteCreateDatabaseDialog.cs \
	Dialogs/SqliteDatabaseConnectionSettingsDialog.cs \
	Dialogs/SqliteTableEditorDialog.cs \
	gtk-gui/generated.cs \
	Schema/dummy.cs \
	SqliteConnectionProvider.cs \
	SqliteDbFactory.cs \
	SqliteDialect.cs \
	SqliteGuiProvider.cs \
	SqlitePooledDbConnection.cs \
	SqliteSchemaProvider.cs \
	Widgets/SqliteConnectionSettingsWidget.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Database.Sql.Sqlite.addin.xml

if ENABLE_SQLITE
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
