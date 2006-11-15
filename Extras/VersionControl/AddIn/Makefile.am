build_sources = VersionControl.AddIn/AddRemoveMoveCommand.cs \
				VersionControl.AddIn/BaseView.cs \
				VersionControl.AddIn/Checkout.cs \
				VersionControl.AddIn/Commands.cs \
				VersionControl.AddIn/CommitCommand.cs \
				VersionControl.AddIn/CommitEventHandler.cs \
				VersionControl.AddIn/FileUpdateEventHandler.cs \
				VersionControl.AddIn/Publish.cs \
				VersionControl.AddIn/RevertCommand.cs \
				VersionControl.AddIn/Task.cs \
				VersionControl.AddIn/Update.cs \
				VersionControl.AddIn/VersionControlNodeExtension.cs \
				VersionControl.AddIn/VersionControlProjectService.cs \
				VersionControl.AddIn.Dialogs/CommitDialog.cs \
				VersionControl.AddIn.Dialogs/EditRepositoryDialog.cs \
				VersionControl.AddIn.Dialogs/SelectRepositoryDialog.cs \
				VersionControl.AddIn.Views/CellRendererDiff.cs \
				VersionControl.AddIn.Views/FileStatusHeader.cs \
				VersionControl.AddIn.Views/Diffs.cs \
				VersionControl.AddIn.Views/Logs.cs \
				VersionControl.AddIn.Views/Statuses.cs \
				gtk-gui/generated.cs

REFS = /r:Mono.Posix.dll \
	   /r:$(ADDIN_BUILD)/Diff.dll \
	   /r:$(ADDIN_BUILD)/DiffWidget.dll \
	   /r:$(ADDIN_BUILD)/VersionControl.dll \
	   /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
       /r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor.dll \
       /r:System.Data.dll \
       $(GTK_SHARP_LIBS) \
       $(GCONF_SHARP_LIBS) \
       $(GTKSOURCEVIEW_SHARP_LIBS)

RES = overlay_added.png \
	  overlay_conflicted.png \
	  overlay_controled.png \
	  overlay_locked.png \
	  overlay_modified.png \
	  overlay_normal.png \
	  overlay_removed.png \
	  overlay_unversioned.png \
	  repository.png \
	  menu_add.png \
	  menu_commit.png \
	  menu_diff.png \
	  menu_log.png \
	  menu_remote_status.png \
	  menu_remove.png \
	  menu_revert.png \
	  menu_status.png \
	  menu_update.png \
	  comment.png \
	  gtk-gui/objects.xml

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
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS) $(RES:%=/resource:%)

CLEANFILES = $(DLL) $(DLL).mdb $(ADDIN_BUILD)/$(ADDIN)

EXTRA_DIST = $(build_sources) $(ADDIN) $(RES)


