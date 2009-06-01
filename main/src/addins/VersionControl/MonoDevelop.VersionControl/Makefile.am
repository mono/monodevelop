
FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.VersionControl.CommitMessageStylePanelWidget.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.CommitDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.EditRepositoryDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.UrlBasedRepositoryEditor.cs \
	MonoDevelop.VersionControl.Dialogs/CommitDialog.cs \
	MonoDevelop.VersionControl.Dialogs/CommitMessageStylePanelWidget.cs \
	MonoDevelop.VersionControl.Dialogs/EditRepositoryDialog.cs \
	MonoDevelop.VersionControl.Dialogs/SelectRepositoryDialog.cs \
	MonoDevelop.VersionControl.Dialogs/VersionControlPolicyPanel.cs \
	MonoDevelop.VersionControl.Views/CellRendererDiff.cs \
	MonoDevelop.VersionControl.Views/DiffView.cs \
	MonoDevelop.VersionControl.Views/LogView.cs \
	MonoDevelop.VersionControl.Views/StatusView.cs \
	MonoDevelop.VersionControl.Views/AnnotateView.cs \
	MonoDevelop.VersionControl/AddRemoveMoveCommand.cs \
	MonoDevelop.VersionControl/AnnotateCommand.cs \
	MonoDevelop.VersionControl/BaseView.cs \
	MonoDevelop.VersionControl/ChangeLogWriter.cs \
	MonoDevelop.VersionControl/ChangeSet.cs \
	MonoDevelop.VersionControl/CheckoutCommand.cs \
	MonoDevelop.VersionControl/Commands.cs \
	MonoDevelop.VersionControl/CommitCommand.cs \
	MonoDevelop.VersionControl/CommitDialogExtension.cs \
	MonoDevelop.VersionControl/CommitEventHandler.cs \
	MonoDevelop.VersionControl/CommitMessageFormat.cs \
	MonoDevelop.VersionControl/CommitMessageStyle.cs \
	MonoDevelop.VersionControl/FileUpdateEventHandler.cs \
	MonoDevelop.VersionControl/LockCommand.cs \
	MonoDevelop.VersionControl/PublishCommand.cs \
	MonoDevelop.VersionControl/Repository.cs \
	MonoDevelop.VersionControl/RevertCommand.cs \
	MonoDevelop.VersionControl/RevertRevisionsCommands.cs \
	MonoDevelop.VersionControl/Revision.cs \
	MonoDevelop.VersionControl/Task.cs \
	MonoDevelop.VersionControl/UnknownRepository.cs \
	MonoDevelop.VersionControl/UnlockCommand.cs \
	MonoDevelop.VersionControl/UpdateCommand.cs \
	MonoDevelop.VersionControl/UrlBasedRepository.cs \
	MonoDevelop.VersionControl/UrlBasedRepositoryEditor.cs \
	MonoDevelop.VersionControl/VersionControlCommandHandler.cs \
	MonoDevelop.VersionControl/VersionControlConfiguration.cs \
	MonoDevelop.VersionControl/VersionControlFileSystemExtension.cs \
	MonoDevelop.VersionControl/VersionControlItem.cs \
	MonoDevelop.VersionControl/VersionControlItemList.cs \
	MonoDevelop.VersionControl/VersionControlNodeExtension.cs \
	MonoDevelop.VersionControl/VersionControlPolicy.cs \
	MonoDevelop.VersionControl/VersionControlService.cs \
	MonoDevelop.VersionControl/VersionControlSystem.cs \
	MonoDevelop.VersionControl/VersionInfo.cs \
	MonoDevelop.VersionControl/VersionStatus.cs

DEPS =  \
	$(top_builddir)/build/AddIns/VersionControl/Diff.dll \
	$(top_builddir)/build/AddIns/VersionControl/DiffWidget.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll 

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Data \
	-r:System.Xml

RES =  \
	comment.png \
	menu_add.png \
	menu_commit.png \
	menu_diff.png \
	menu_log.png \
	menu_remote_status.png \
	menu_remove.png \
	menu_resolve.png \
	menu_revert.png \
	menu_status.png \
	menu_update.png \
	overlay_added.png \
	overlay_conflicted.png \
	overlay_controled.png \
	overlay_lock_required.png \
	overlay_locked.png \
	overlay_modified.png \
	overlay_normal.png \
	overlay_removed.png \
	overlay_unlocked.png \
	overlay_unversioned.png \
	Policies.xml \
	repository.png \
	VersionControl.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl

DLL = $(ADDIN_BUILD)/MonoDevelop.VersionControl.dll

if ENABLE_VERSIONCONTROL
all: $(DLL) $(DLL).mdb
else
all:
endif

if ENABLE_VERSIONCONTROL
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(DLL) $(DLL).mdb
endif

$(DLL): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(REFS) $(build_deps) $(build_sources) $(build_resources:%=/resource:%)

$(DLL).mdb: $(DLL)

CLEANFILES = $(DLL) $(DLL).mdb

EXTRA_DIST = $(build_sources) $(RES)

include $(top_srcdir)/Makefile.include

