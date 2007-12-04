ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Sql.SqlServer.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../build/MonoDevelop.Database.Components.dll \
	-r:../build/MonoDevelop.Database.Designer.dll \
	-r:../build/MonoDevelop.Database.Sql.dll \
	-r:System \
	-r:System.Data

FILES =  \
	SqlServerConnectionProvider.cs \
	SqlServerDbFactory.cs \
	SqlServerDialect.cs \
	SqlServerGuiProvider.cs \
	SqlServerPooledDbConnection.cs \
	SqlServerSchemaProvider.cs

RES = MonoDevelop.Database.Sql.SqlServer.addin.xml

if ENABLE_SQLSERVER
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
