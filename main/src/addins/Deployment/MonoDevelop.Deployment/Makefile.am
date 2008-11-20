
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Deployment
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Deployment.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
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
	-r:ICSharpCode.SharpZipLib \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml 

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Deployment.DeployFileListWidget.cs \
	gtk-gui/MonoDevelop.Deployment.FileReplaceDialog.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.BinariesZipEditorWidget.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.CommandDeployEditorWidget.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.DeployDialog.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.EditPackageDialog.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.EntrySelectionTree.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.FileCopyConfigurationSelector.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.InstallDialog.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.LocalFileCopyConfigurationEditorWidget.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.PackagingFeatureWidget.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.SourcesZipEditorWidget.cs \
	gtk-gui/MonoDevelop.Deployment.Gui.SshFuseFileCopyConfigurationEditorWidget.cs \
	MonoDevelop.Deployment.Commands/Commands.cs \
	MonoDevelop.Deployment.Gui/BinariesZipEditorWidget.cs \
	MonoDevelop.Deployment.Gui/CommandDeployEditorWidget.cs \
	MonoDevelop.Deployment.Gui/DeployDialog.cs \
	MonoDevelop.Deployment.Gui/DeployDirectoryInfoEditor.cs \
	MonoDevelop.Deployment.Gui/DeployFileListWidget.cs \
	MonoDevelop.Deployment.Gui/DeployOperations.cs \
	MonoDevelop.Deployment.Gui/DialogFileReplacePolicy.cs \
	MonoDevelop.Deployment.Gui/EditPackageDialog.cs \
	MonoDevelop.Deployment.Gui/EntrySelectionTree.cs \
	MonoDevelop.Deployment.Gui/FileCopyConfigurationSelector.cs \
	MonoDevelop.Deployment.Gui/FileReplaceDialog.cs \
	MonoDevelop.Deployment.Gui/IFileCopyConfigurationEditor.cs \
	MonoDevelop.Deployment.Gui/InstallDialog.cs \
	MonoDevelop.Deployment.Gui/IPackageBuilderEditor.cs \
	MonoDevelop.Deployment.Gui/LocalFileCopyConfigurationEditor.cs \
	MonoDevelop.Deployment.Gui/LocalFileCopyConfigurationEditorWidget.cs \
	MonoDevelop.Deployment.Gui/PackageBuilderEditor.cs \
	MonoDevelop.Deployment.Gui/PackagingFeature.cs \
	MonoDevelop.Deployment.Gui/PackagingFeatureWidget.cs \
	MonoDevelop.Deployment.Gui/PropertyProvider.cs \
	MonoDevelop.Deployment.Gui/SourcesZipEditorWidget.cs \
	MonoDevelop.Deployment.Gui/SshFuseFileCopyConfigurationEditor.cs \
	MonoDevelop.Deployment.Gui/SshFuseFileCopyConfigurationEditorWidget.cs \
	MonoDevelop.Deployment.NodeBuilders/PackageNodeBuilder.cs \
	MonoDevelop.Deployment.NodeBuilders/PackagingProjectNodeBuilder.cs \
	MonoDevelop.Deployment.Targets/BaseFuseFileCopyConfiguration.cs \
	MonoDevelop.Deployment.Targets/BaseFuseFileCopyHandler.cs \
	MonoDevelop.Deployment.Targets/BinariesZipPackageBuilder.cs \
	MonoDevelop.Deployment.Targets/CommandPackageBuilder.cs \
	MonoDevelop.Deployment.Targets/LocalFileCopyConfiguration.cs \
	MonoDevelop.Deployment.Targets/LocalFileCopyHandler.cs \
	MonoDevelop.Deployment.Targets/SourcesZipPackageBuilder.cs \
	MonoDevelop.Deployment.Targets/SshFuseFileCopyHandler.cs \
	MonoDevelop.Deployment/DefaultDeployServiceExtension.cs \
	MonoDevelop.Deployment/DeployContext.cs \
	MonoDevelop.Deployment/DeployDirectoryInfo.cs \
	MonoDevelop.Deployment/DeployDirectoryNodeType.cs \
	MonoDevelop.Deployment/DeployFile.cs \
	MonoDevelop.Deployment/DeployPlatformInfo.cs \
	MonoDevelop.Deployment/DeployPlatformNodeType.cs \
	MonoDevelop.Deployment/DeployProperties.cs \
	MonoDevelop.Deployment/DeployService.cs \
	MonoDevelop.Deployment/DeployServiceExtension.cs \
	MonoDevelop.Deployment/FileCopyConfiguration.cs \
	MonoDevelop.Deployment/FileCopyHandler.cs \
	MonoDevelop.Deployment/IDeployable.cs \
	MonoDevelop.Deployment/IDeployDirectoryResolver.cs \
	MonoDevelop.Deployment/IFileCopyHandler.cs \
	MonoDevelop.Deployment/IFileReplacePolicy.cs \
	MonoDevelop.Deployment/InstallResolver.cs \
	MonoDevelop.Deployment/MD1Serializer.cs \
	MonoDevelop.Deployment/Package.cs \
	MonoDevelop.Deployment/PackageBuilder.cs \
	MonoDevelop.Deployment/PackageCollection.cs \
	MonoDevelop.Deployment/PackagingProject.cs \
	MonoDevelop.Deployment/TargetDirectory.cs \
	MonoDevelop.Deployment/UnknownFileCopyConfiguration.cs \
	MonoDevelop.Deployment/UnknownPackageBuilder.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/package-x-generic.png \
	icons/packaging-16.png \
	icons/packaging-32.png \
	md1format.xml \
	MonoDevelop.Deployment.addin.xml \
	templates/PackagingProject.xpt.xml

DATA_FILES = 

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(DATA_FILES))

all: $(ASSEMBLY) $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)/$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Deployment
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
