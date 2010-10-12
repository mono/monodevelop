
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.VersionControl.Git.dll

DEPS =  \
	$(top_builddir)/build/AddIns/VersionControl/MonoDevelop.VersionControl.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:../../../../../../ngit/bin/NGit.dll \
	-r:../../../../../../ngit/bin/Sharpen.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.VersionControl.Git.ConflictResolutionDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Git.EditBranchDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Git.EditRemoteDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Git.GitConfigurationDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Git.MergeDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Git.PushDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Git.UserInfoConflictDialog.cs \
	MonoDevelop.VersionControl.Git/Change.cs \
	MonoDevelop.VersionControl.Git/Commands.cs \
	MonoDevelop.VersionControl.Git/CommitDialogExtensionWidget.cs \
	MonoDevelop.VersionControl.Git/ConflictResolutionDialog.cs \
	MonoDevelop.VersionControl.Git/EditBranchDialog.cs \
	MonoDevelop.VersionControl.Git/EditRemoteDialog.cs \
	MonoDevelop.VersionControl.Git/GitClient.cs \
	MonoDevelop.VersionControl.Git/GitConfigurationDialog.cs \
	MonoDevelop.VersionControl.Git/GitNodeBuilderExtension.cs \
	MonoDevelop.VersionControl.Git/GitRepository.cs \
	MonoDevelop.VersionControl.Git/GitService.cs \
	MonoDevelop.VersionControl.Git/GitUtil.cs \
	MonoDevelop.VersionControl.Git/GitVersionControl.cs \
	MonoDevelop.VersionControl.Git/MergeDialog.cs \
	MonoDevelop.VersionControl.Git/PushDialog.cs \
	MonoDevelop.VersionControl.Git/RepositoryStatus.cs \
	MonoDevelop.VersionControl.Git/Stash.cs \
	MonoDevelop.VersionControl.Git/UserInfoConflictDialog.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.VersionControl.Git.addin.xml

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) $(build_deps) $(build_resources:%=/resource:%) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

if ENABLE_GIT
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

if ENABLE_GIT
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

include $(top_srcdir)/Makefile.include
