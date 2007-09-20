ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Database
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Sql.Sqlite.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Database/MonoDevelop.Database.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Database/MonoDevelop.Database.Designer.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Database/MonoDevelop.Database.Sql.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_srcdir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_srcdir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_srcdir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_srcdir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/build/bin/Mono.Addins.dll \
	-r:$(top_srcdir)/contrib/Sqlite/Mono.Data.Sqlite.dll \
	-r:Mono.Data \
	-r:System \
	-r:System.Data

FILES =  \
	SqliteConnectionProvider.cs \
	SqliteDbFactory.cs \
	SqliteDialect.cs \
	SqliteGuiProvider.cs \
	SqlitePooledDbConnection.cs \
	SqliteSchemaProvider.cs

RES = MonoDevelop.Database.Sql.Sqlite.addin.xml

SQLITE_FILES = \
	Mono.Data.Sqlite.dll

SRC_SQLITE_FILES = $(addprefix $(top_builddir)/contrib/Sqlite/, $(SQLITE_FILES))
BUILD_SQLITE_FILES = $(addprefix $(ADDIN_BUILD)/, $(notdir $(SQLITE_FILES)))

ADDIN = MonoDevelop.Database.Sql.Sqlite.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(BUILD_SQLITE_FILES)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(BUILD_SQLITE_FILES): $(SRC_SQLITE_FILES)
	mkdir -p $(ADDIN_BUILD)
	cp $(SRC_SQLITE_FILES) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ADDIN) $(SRC_SQLITE_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(BUILD_SQLITE_FILES)
EXTRA_DIST = $(SRC_SQLITE_FILES)

include $(top_srcdir)/Makefile.include
