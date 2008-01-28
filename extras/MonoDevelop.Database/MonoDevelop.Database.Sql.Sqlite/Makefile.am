ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Sql.Sqlite.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:$(top_builddir)/build/MonoDevelop.Database.Components.dll \
	-r:$(top_builddir)/build/MonoDevelop.Database.Designer.dll \
	-r:$(top_builddir)/build/MonoDevelop.Database.Sql.dll \
	-r:$(top_srcdir)/contrib/Sqlite/Mono.Data.Sqlite.dll \
	-r:Mono.Data \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data

FILES =  \
	Dialogs/SqliteCreateDatabaseDialog.cs \
	Dialogs/SqliteDatabaseConnectionSettingsDialog.cs \
	Dialogs/SqliteTableEditorDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Database.Sql.Sqlite.SqliteCreateDatabaseDialog.cs \
	Schema/dummy.cs \
	SqliteConnectionProvider.cs \
	SqliteDbFactory.cs \
	SqliteDialect.cs \
	SqliteGuiProvider.cs \
	SqlitePooledDbConnection.cs \
	SqliteSchemaProvider.cs \
	Widgets/dummy.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Database.Sql.Sqlite.addin.xml

SQLITE_FILES = \
	Mono.Data.Sqlite.dll

SRC_SQLITE_FILES = $(addprefix $(top_srcdir)/contrib/Sqlite/, $(SQLITE_FILES))
BUILD_SQLITE_FILES = $(addprefix $(ADDIN_BUILD)/, $(notdir $(SQLITE_FILES)))

all: $(ASSEMBLY) $(BUILD_SQLITE_FILES)

$(BUILD_SQLITE_FILES): $(SRC_SQLITE_FILES)
	mkdir -p $(ADDIN_BUILD)
	cp $(SRC_SQLITE_FILES) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(BUILD_SQLITE_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(BUILD_SQLITE_FILES)
EXTRA_DIST = $(FILES) $(RES) $(SRC_SQLITE_FILES)

include $(top_srcdir)/Makefile.include
