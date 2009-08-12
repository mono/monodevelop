

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=.

include $(top_srcdir)/Makefile.include
include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG
ASSEMBLY = build/MonoDevelop.AddinAuthoring.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build


endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = bin/Release/MonoDevelop.AddinAuthoring.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release


endif


LINUX_PKGCONFIG = \
	$(MONODEVELOP_ADDINAUTHORING_PC)  



MONODEVELOP_ADDINAUTHORING_PC = $(BUILD_DIR)/monodevelop.addinauthoring.pc


FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.AddinDescriptionWidget.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.AddinFeatureWidget.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.AddinOptionPanelWidget.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.ExtensionEditorWidget.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.ExtensionPointsEditorWidget.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.ExtensionSelectorDialog.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.NewExtensionPointDialog.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.NewRegistryDialog.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.NodeSetEditorDialog.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.NodeSetEditorWidget.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.NodeTypeEditorDialog.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.RegistrySelector.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.SelectNodeSetDialog.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.SelectRepositoryDialog.cs \
	gtk-gui/MonoDevelop.AddinAuthoring.TypeSelector.cs \
	MonoDevelop.AddinAuthoring.CodeCompletion/CodeCompletionExtension.cs \
	MonoDevelop.AddinAuthoring.NodeBuilders/AddinFolderNodeBuilder.cs \
	MonoDevelop.AddinAuthoring.NodeBuilders/AddinHeaderNodeBuilder.cs \
	MonoDevelop.AddinAuthoring.NodeBuilders/AddinReferenceNodeBuilder.cs \
	MonoDevelop.AddinAuthoring.NodeBuilders/ExtensionPointsNodeBuilder.cs \
	MonoDevelop.AddinAuthoring.NodeBuilders/ExtensionsNodeBuilder.cs \
	MonoDevelop.AddinAuthoring.NodeBuilders/ProjectFolderNodeBuilderExtension.cs \
	MonoDevelop.AddinAuthoring.NodeBuilders/ReferenceNodeBuilder.cs \
	MonoDevelop.AddinAuthoring.NodeBuilders/ReferencesFolderNodeBuilder.cs \
	MonoDevelop.AddinAuthoring/AddinAuthoringService.cs \
	MonoDevelop.AddinAuthoring/AddinData.cs \
	MonoDevelop.AddinAuthoring/AddinDescriptionDisplayBinding.cs \
	MonoDevelop.AddinAuthoring/AddinDescriptionView.cs \
	MonoDevelop.AddinAuthoring/AddinDescriptionWidget.cs \
	MonoDevelop.AddinAuthoring/AddinFeatureWidget.cs \
	MonoDevelop.AddinAuthoring/AddinFileDescriptionTemplate.cs \
	MonoDevelop.AddinAuthoring/AddinOptionPanelWidget.cs \
	MonoDevelop.AddinAuthoring/AddinProjectExtension.cs \
	MonoDevelop.AddinAuthoring/AddinProjectReference.cs \
	MonoDevelop.AddinAuthoring/CellRendererExtension.cs \
	MonoDevelop.AddinAuthoring/Commands.cs \
	MonoDevelop.AddinAuthoring/ExtensionDomain.cs \
	MonoDevelop.AddinAuthoring/ExtensionEditorWidget.cs \
	MonoDevelop.AddinAuthoring/ExtensionPointsEditorWidget.cs \
	MonoDevelop.AddinAuthoring/ExtensionSelectorDialog.cs \
	MonoDevelop.AddinAuthoring/NewExtensionPointDialog.cs \
	MonoDevelop.AddinAuthoring/NewRegistryDialog.cs \
	MonoDevelop.AddinAuthoring/NodeEditorWidget.cs \
	MonoDevelop.AddinAuthoring/NodeSetEditorDialog.cs \
	MonoDevelop.AddinAuthoring/NodeSetEditorWidget.cs \
	MonoDevelop.AddinAuthoring/NodeTypeEditorDialog.cs \
	MonoDevelop.AddinAuthoring/RegistryExtensionNode.cs \
	MonoDevelop.AddinAuthoring/RegistrySelector.cs \
	MonoDevelop.AddinAuthoring/SelectNodeSetDialog.cs \
	MonoDevelop.AddinAuthoring/SelectRepositoryDialog.cs \
	MonoDevelop.AddinAuthoring/SolutionAddinData.cs \
	MonoDevelop.AddinAuthoring/TypeCellEditor.cs \
	MonoDevelop.AddinAuthoring/TypeSelector.cs 

DATA_FILES = 

RESOURCES =  \
	extension-node-set.png \
	extension-node-type.png \
	extension-point.png \
	flare.png \
	gtk-gui/gui.stetic \
	MonoDevelop.AddinAuthoring.addin.xml \
	templates/AddinProject.xpt.xml \
	templates/ExtensibleApplicationProject.xpt.xml \
	templates/ExtensibleLibraryProject.xpt.xml 

EXTRAS = \
	monodevelop.addinauthoring.pc.in 

REFERENCES =  \
	Mono.Posix \
	-pkg:gtk-sharp-2.0 \
	-pkg:mono-addins \
	-pkg:mono-addins-setup \
	-pkg:monodevelop \
	-pkg:monodevelop-core-addins \
	System \
	System.Core \
	System.Xml

DLL_REFERENCES = 

CLEANFILES += $(LINUX_PKGCONFIG) 

#Targets
all-local: $(ASSEMBLY) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make

$(MONODEVELOP_ADDINAUTHORING_PC): monodevelop.addinauthoring.pc
	mkdir -p $(BUILD_DIR)
	cp '$<' '$@'



monodevelop.addinauthoring.pc: monodevelop.addinauthoring.pc.in $(top_srcdir)/config.make
	sed -e "s,@prefix@,$(prefix)," -e "s,@PACKAGE@,$(PACKAGE)," < monodevelop.addinauthoring.pc.in > monodevelop.addinauthoring.pc


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

LOCAL_PKGCONFIG=PKG_CONFIG_PATH=../../local-config:$$PKG_CONFIG_PATH

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(dir $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(LOCAL_PKGCONFIG) $(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)


install-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(MONODEVELOP_ADDINAUTHORING_PC)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p $(DESTDIR)$(prefix)/lib/$(PACKAGE)
	cp $(ASSEMBLY) $(ASSEMBLY_MDB) $(DESTDIR)$(prefix)/lib/$(PACKAGE)
	mkdir -p $(DESTDIR)$(prefix)/lib/pkgconfig
	test -z '$(MONODEVELOP_ADDINAUTHORING_PC)' || cp $(MONODEVELOP_ADDINAUTHORING_PC) $(DESTDIR)$(prefix)/lib/pkgconfig
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(MONODEVELOP_ADDINAUTHORING_PC)
	make pre-uninstall-local-hook prefix=$(prefix)
	rm -f $(DESTDIR)$(prefix)/lib/$(PACKAGE)/$(notdir $(ASSEMBLY))
	test -z '$(ASSEMBLY_MDB)' || rm -f $(DESTDIR)$(prefix)/lib/$(PACKAGE)/$(notdir $(ASSEMBLY_MDB))
	test -z '$(MONODEVELOP_ADDINAUTHORING_PC)' || rm -f $(DESTDIR)$(prefix)/lib/pkgconfig/$(notdir $(MONODEVELOP_ADDINAUTHORING_PC))
	make post-uninstall-local-hook prefix=$(prefix)
