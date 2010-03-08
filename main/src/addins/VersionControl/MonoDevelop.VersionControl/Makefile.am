
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
	MonoDevelop.VersionControl.Views/AnnotateView.cs \
	MonoDevelop.VersionControl.Views/CellRendererDiff.cs \
	MonoDevelop.VersionControl.Views/DiffParser.cs \
	MonoDevelop.VersionControl.Views/DiffView.cs \
	MonoDevelop.VersionControl.Views/LogView.cs \
	MonoDevelop.VersionControl.Views/StatusView.cs \
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
	MonoDevelop.VersionControl/CreatePatchCommand.cs \
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
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

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
	gtk-gui/gui.stetic \
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
	VersionControl.addin.xml \
	version-control-16.png

DATA_FILES =  \
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
	menu_update.png

ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(DATA_FILES))

DLL = $(ADDIN_BUILD)/MonoDevelop.VersionControl.dll

all: $(DLL) $(DLL).mdb $(DATA_FILE_BUILD)

addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(DLL) $(DLL).mdb $(DATA_FILES)

$(DATA_FILE_BUILD): $(srcdir)/$(notdir $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(notdir $@) $(ADDIN_BUILD)/.

$(DLL): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(REFS) $(build_deps) $(build_sources) $(build_resources:%=/resource:%)

$(DLL).mdb: $(DLL)

CLEANFILES = $(DLL) $(DLL).mdb $(DATA_FILE_BUILD)

EXTRA_DIST = $(build_sources) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include

