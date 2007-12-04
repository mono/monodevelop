ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Sql.Npgsql.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../build/MonoDevelop.Database.Components.dll \
	-r:../build/MonoDevelop.Database.Designer.dll \
	-r:../build/MonoDevelop.Database.Sql.dll \
	-r:Npgsql \
	-r:System \
	-r:System.Data

FILES =  \
	NpgsqlConnectionProvider.cs \
	NpgsqlDbFactory.cs \
	NpgsqlDialect.cs \
	NpgsqlGuiProvider.cs \
	NpgsqlPooledDbConnection.cs \
	NpgsqlSchemaProvider.cs

RES = MonoDevelop.Database.Sql.Npgsql.addin.xml

if ENABLE_POSTGRES
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


if ENABLE_POSTGRES
assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
