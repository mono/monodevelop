ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Sql.MySql.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:$(top_builddir)/build/MonoDevelop.Database.Components.dll \
	-r:$(top_builddir)/build/MonoDevelop.Database.Designer.dll \
	-r:$(top_builddir)/build/MonoDevelop.Database.Sql.dll \
	-r:$(top_srcdir)/contrib/MySql/MySql.Data.dll \
	-r:System \
	-r:System.Data

FILES =  \
	MySqlConnectionProvider.cs \
	MySqlDbFactory.cs \
	MySqlDialect.cs \
	MySqlGuiProvider.cs \
	MySqlPooledDbConnection.cs \
	MySqlSchemaProvider.cs

MYSQL_FILES = \
	MySql.Data.dll

RES = MonoDevelop.Database.Sql.MySql.addin.xml

SRC_MYSQL_FILES = $(addprefix $(top_srcdir)/contrib/MySql/, $(MYSQL_FILES))
BUILD_MYSQL_FILES = $(addprefix $(ADDIN_BUILD)/, $(notdir $(MYSQL_FILES)))

all: $(ASSEMBLY) $(BUILD_MYSQL_FILES)

$(BUILD_MYSQL_FILES): $(SRC_MYSQL_FILES)
	mkdir -p $(ADDIN_BUILD)
	cp $(SRC_MYSQL_FILES) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(BUILD_MYSQL_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(BUILD_MYSQL_FILES)
EXTRA_DIST = $(FILES) $(RES) $(SRC_MYSQL_FILES)

include $(top_srcdir)/Makefile.include
