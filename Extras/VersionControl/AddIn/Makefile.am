build_sources = Addin.cs Diffs.cs Logs.cs Statuses.cs Task.cs Update.cs

REFS = /r:$(ADDIN_BUILD)/Diff.dll \
	   /r:$(ADDIN_BUILD)/DiffWidget.dll \
	   /r:$(ADDIN_BUILD)/VersionControl.dll \
	   /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
       /r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
       /r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
       /r:$(top_builddir)/build/bin/MonoDevelop.SourceEditor.dll \
       /r:System.Data.dll \
       $(GTK_SHARP_LIBS) \
       $(GLADE_SHARP_LIBS) \
       $(GCONF_SHARP_LIBS) \
       $(GTKSOURCEVIEW_SHARP_LIBS)

RES = -resource:$(srcdir)/overlay_added.png,overlay_added.png \
	  -resource:$(srcdir)/overlay_modified.png,overlay_modified.png \
	  -resource:$(srcdir)/overlay_conflicted.png,overlay_conflicted.png \
	  -resource:$(srcdir)/overlay_normal.png,overlay_normal.png \
	  -resource:$(srcdir)/overlay_locked.png,overlay_locked.png

ADDIN = VersionControl.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl

DLL = $(ADDIN_BUILD)/VersionControlAddIn.dll

if ENABLE_VERSIONCONTROL
all: $(ADDIN_BUILD)/$(ADDIN) $(DLL)
else
all:
endif

if ENABLE_VERSIONCONTROL
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(srcdir)/$(ADDIN) $(DLL)
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(DLL): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS) $(RES)

CLEANFILES = $(DLL) $(DLL).mdb $(ADDIN_BUILD)/$(ADDIN)

EXTRA_DIST = $(build_sources) $(ADDIN) overlay_added.png overlay_modified.png overlay_conflicted.png overlay_normal.png overlay_locked.png


