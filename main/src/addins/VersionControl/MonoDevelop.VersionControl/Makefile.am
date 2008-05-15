
FILES =  \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.CommitDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.EditRepositoryDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.UrlBasedRepositoryEditor.cs \
	MonoDevelop.VersionControl.Dialogs/CommitDialog.cs \
	MonoDevelop.VersionControl.Dialogs/EditRepositoryDialog.cs \
	MonoDevelop.VersionControl.Dialogs/SelectRepositoryDialog.cs \
	MonoDevelop.VersionControl.Views/CellRendererDiff.cs \
	MonoDevelop.VersionControl.Views/Diffs.cs \
	MonoDevelop.VersionControl.Views/Logs.cs \
	MonoDevelop.VersionControl.Views/Statuses.cs \
	MonoDevelop.VersionControl/AddRemoveMoveCommand.cs \
	MonoDevelop.VersionControl/BaseView.cs \
	MonoDevelop.VersionControl/ChangeSet.cs \
	MonoDevelop.VersionControl/Checkout.cs \
	MonoDevelop.VersionControl/Commands.cs \
	MonoDevelop.VersionControl/CommitCommand.cs \
	MonoDevelop.VersionControl/CommitDialogExtension.cs \
	MonoDevelop.VersionControl/CommitEventHandler.cs \
	MonoDevelop.VersionControl/FileUpdateEventHandler.cs \
	MonoDevelop.VersionControl/Publish.cs \
	MonoDevelop.VersionControl/Repository.cs \
	MonoDevelop.VersionControl/RevertCommand.cs \
	MonoDevelop.VersionControl/Revision.cs \
	MonoDevelop.VersionControl/Task.cs \
	MonoDevelop.VersionControl/UnknownRepository.cs \
	MonoDevelop.VersionControl/Update.cs \
	MonoDevelop.VersionControl/UrlBasedRepository.cs \
	MonoDevelop.VersionControl/UrlBasedRepositoryEditor.cs \
	MonoDevelop.VersionControl/VersionControlConfiguration.cs \
	MonoDevelop.VersionControl/VersionControlFileSystemExtension.cs \
	MonoDevelop.VersionControl/VersionControlNodeExtension.cs \
	MonoDevelop.VersionControl/VersionControlService.cs \
	MonoDevelop.VersionControl/VersionControlSystem.cs \
	MonoDevelop.VersionControl/VersionInfo.cs \
	MonoDevelop.VersionControl/VersionStatus.cs

REFS =  \
	$(GCONF_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/AddIns/VersionControl/Diff.dll \
	-r:$(top_builddir)/build/AddIns/VersionControl/DiffWidget.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data \
	-r:System.Xml

RES =  \
	comment.png \
	gtk-gui/objects.xml \
	menu_add.png \
	menu_commit.png \
	menu_diff.png \
	menu_log.png \
	menu_remote_status.png \
	menu_remove.png \
	menu_revert.png \
	menu_status.png \
	menu_update.png \
	overlay_added.png \
	overlay_conflicted.png \
	overlay_controled.png \
	overlay_locked.png \
	overlay_modified.png \
	overlay_normal.png \
	overlay_removed.png \
	overlay_unversioned.png \
	repository.png \
	VersionControl.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl

DLL = $(ADDIN_BUILD)/MonoDevelop.VersionControl.dll

if ENABLE_VERSIONCONTROL
all: $(DLL)
else
all:
endif

if ENABLE_VERSIONCONTROL
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(DLL) $(DLL).mdb
endif

$(DLL): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(REFS) $(build_sources) $(build_resources:%=/resource:%)

CLEANFILES = $(DLL) $(DLL).mdb

EXTRA_DIST = $(build_sources) $(RES)

include $(top_srcdir)/Makefile.include

