ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Query.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:$(top_builddir)/build/MonoDevelop.Database.Components.dll \
	-r:$(top_builddir)/build/MonoDevelop.Database.Sql.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data

RES =  \
	gtk-gui/gui.stetic \
	icons/Icons.16x16.Database.Column.Key.png \
	icons/Icons.16x16.Database.Column.png \
	icons/Icons.16x16.Database.Connection.Error.png \
	icons/Icons.16x16.Database.Connection.png \
	icons/Icons.16x16.Database.Connection.Remove.png \
	icons/Icons.16x16.Database.Constraints.png \
	icons/Icons.16x16.Database.Database.Error.png \
	icons/Icons.16x16.Database.Database.Ok.png \
	icons/Icons.16x16.Database.Database.png \
	icons/Icons.16x16.Database.Execute.png \
	icons/Icons.16x16.Database.Parameter.png \
	icons/Icons.16x16.Database.Procedure.Error.png \
	icons/Icons.16x16.Database.Procedure.png \
	icons/Icons.16x16.Database.Procedure.Source.png \
	icons/Icons.16x16.Database.Relations.png \
	icons/Icons.16x16.Database.Table.Error.png \
	icons/Icons.16x16.Database.Table.png \
	icons/Icons.16x16.Database.Tables.png \
	icons/Icons.16x16.Database.User.png \
	icons/Icons.16x16.Database.Users.png \
	icons/Icons.16x16.Database.View.Error.png \
	icons/Icons.16x16.Database.View.png \
	icons/Icons.16x16.Database.View.Source.png \
	icons/Icons.16x16.Database.Views.png \
	MonoDevelop.Database.Query.addin.xml

FILES =  \
	AssemblyInfo.cs \
	Commands/ToolCommands.cs \
	gtk-gui/generated.cs \
	QueryResultView.cs \
	SqlQueryView.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
