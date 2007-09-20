ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Database
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Query.dll
REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Database/MonoDevelop.Database.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Database/MonoDevelop.Database.Sql.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data

RES =  \
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
	Commands/ToolCommands.cs \
	QueryResultView.cs \
	SqlQueryView.cs

ADDIN = MonoDevelop.Database.Query.addin.xml

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
