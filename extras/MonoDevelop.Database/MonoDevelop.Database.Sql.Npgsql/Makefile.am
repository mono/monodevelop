ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Sql.Npgsql.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../build/MonoDevelop.Database.Components.dll \
	-r:../build/MonoDevelop.Database.Designer.dll \
	-r:../build/MonoDevelop.Database.Sql.dll \
	-r:Mono.Posix \
	-r:Npgsql \
	-r:System \
	-r:System.Data

FILES =  \
	Dialogs/NpgsqlTableEditorDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Database.Sql.Npgsql.NpgsqlColumnEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Sql.Npgsql.NpgsqlCreateDatabaseWidget.cs \
	NpgsqlConnectionProvider.cs \
	NpgsqlDbFactory.cs \
	NpgsqlDialect.cs \
	NpgsqlGuiProvider.cs \
	NpgsqlPooledDbConnection.cs \
	NpgsqlSchemaProvider.cs \
	Schema/dummy.cs \
	Widgets/NpgsqlColumnEditorWidget.cs \
	Widgets/NpgsqlCreateDatabaseWidget.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.Database.Sql.Npgsql.addin.xml

if ENABLE_POSTGRES
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


if ENABLE_POSTGRES
assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
