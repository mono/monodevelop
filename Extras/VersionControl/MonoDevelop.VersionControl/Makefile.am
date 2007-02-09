
FILES = \
	MonoDevelop.VersionControl/AddRemoveMoveCommand.cs \
	MonoDevelop.VersionControl/BaseView.cs \
	MonoDevelop.VersionControl/Checkout.cs \
	MonoDevelop.VersionControl/Commands.cs \
	MonoDevelop.VersionControl/CommitCommand.cs \
	MonoDevelop.VersionControl/CommitEventHandler.cs \
	MonoDevelop.VersionControl/FileUpdateEventHandler.cs \
	MonoDevelop.VersionControl/Publish.cs \
	MonoDevelop.VersionControl/RevertCommand.cs \
	MonoDevelop.VersionControl/Task.cs \
	MonoDevelop.VersionControl/Update.cs \
	MonoDevelop.VersionControl/ChangeSet.cs \
	MonoDevelop.VersionControl/VersionControlSystem.cs \
	MonoDevelop.VersionControl/VersionControlConfiguration.cs \
	MonoDevelop.VersionControl/Revision.cs \
	MonoDevelop.VersionControl/UnknownRepository.cs \
	MonoDevelop.VersionControl/Repository.cs \
	MonoDevelop.VersionControl/UrlBasedRepository.cs \
	MonoDevelop.VersionControl/VersionInfo.cs \
	MonoDevelop.VersionControl/VersionControlService.cs \
	MonoDevelop.VersionControl/UrlBasedRepositoryEditor.cs \
	MonoDevelop.VersionControl/VersionStatus.cs \
	MonoDevelop.VersionControl/CommitDialogExtension.cs \
	MonoDevelop.VersionControl/VersionControlNodeExtension.cs \
	MonoDevelop.VersionControl/VersionControlProjectService.cs \
	MonoDevelop.VersionControl/VersionControlFileSystemExtension.cs \
	MonoDevelop.VersionControl.Dialogs/CommitDialog.cs \
	MonoDevelop.VersionControl.Dialogs/EditRepositoryDialog.cs \
	MonoDevelop.VersionControl.Dialogs/SelectRepositoryDialog.cs \
	MonoDevelop.VersionControl.Views/CellRendererDiff.cs \
	MonoDevelop.VersionControl.Views/Diffs.cs \
	MonoDevelop.VersionControl.Views/Logs.cs \
	MonoDevelop.VersionControl.Views/Statuses.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.CommitDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.EditRepositoryDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.UrlBasedRepositoryEditor.cs \
	gtk-gui/generated.cs

REFS = /r:Mono.Posix.dll \
	   /r:$(ADDIN_BUILD)/Diff.dll \
	   /r:$(ADDIN_BUILD)/DiffWidget.dll \
	   /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
       /r:System.Data.dll \
       $(GTK_SHARP_LIBS) \
       $(GCONF_SHARP_LIBS)

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

$(DLL): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS) $(build_resources:%=/resource:%)

CLEANFILES = $(DLL) $(DLL).mdb $(ADDIN_BUILD)/$(ADDIN)

EXTRA_DIST = $(build_sources) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include

