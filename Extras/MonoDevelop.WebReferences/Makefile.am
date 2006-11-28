
ADDIN = MonoDevelop.WebReferences.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.WebReferences
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.WebReferences.dll

REFS =  \
	-r:System \
	-r:System.Web \
	-r:System.Web.Services \
	-r:System.Xml \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll

FILES = \
AssemblyInfo.cs \
gtk-gui/generated.cs \
MonoDevelop.WebReferences/CodeGenerator.cs \
MonoDevelop.WebReferences/DiscoveryNetworkCredential.cs \
MonoDevelop.WebReferences/DiscoveryProtocol.cs \
MonoDevelop.WebReferences/Library.cs \
MonoDevelop.WebReferences/WebReferenceFolder.cs \
MonoDevelop.WebReferences/WebReferenceItem.cs \
MonoDevelop.WebReferences/WebReferenceItemCollection.cs \
MonoDevelop.WebReferences.Commands/WebReferenceCommands.cs \
MonoDevelop.WebReferences.Commands/WebReferenceCommandHandler.cs \
MonoDevelop.WebReferences.Dialogs/WebReferenceDialog.cs \
MonoDevelop.WebReferences.NodeBuilders/ProjectFolderNodeBuilderExtension.cs \
MonoDevelop.WebReferences.NodeBuilders/ProjectNodeBuilder.cs \
MonoDevelop.WebReferences.NodeBuilders/WebReferenceFolderNodeBuilder.cs \
MonoDevelop.WebReferences.NodeBuilders/WebReferenceNodeBuilder.cs

RES = \
MonoDevelop.WebReferences.Resources/disco.html \
MonoDevelop.WebReferences.Resources/wsdl.html \
MonoDevelop.WebReferences.Resources/WebReferenceItem.png \
MonoDevelop.WebReferences.Resources/WebReference.File.Disco.png \
MonoDevelop.WebReferences.Resources/WebReference.File.Wsdl.png \
MonoDevelop.WebReferences.Resources/WebReference.Service.png

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $@

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library \
	$(RES:%=/resource:%) $(build_sources) $(REFS)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.WebReferences
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN)
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include
