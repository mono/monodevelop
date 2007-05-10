
ADDIN = MonoDevelop.WebReferences.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.WebReferences
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.WebReferences.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Web \
	-r:System.Web.Services \
	-r:System.Xml

FILES =  \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/gtk-gui/generated.cs \
	$(srcdir)/gtk-gui/MonoDevelop.WebReferences.Dialogs.WebReferenceDialog.cs \
	$(srcdir)/MonoDevelop.WebReferences.Commands/WebReferenceCommandHandler.cs \
	$(srcdir)/MonoDevelop.WebReferences.Commands/WebReferenceCommands.cs \
	$(srcdir)/MonoDevelop.WebReferences.Dialogs/WebReferenceDialog.cs \
	$(srcdir)/MonoDevelop.WebReferences.NodeBuilders/ProjectFolderNodeBuilderExtension.cs \
	$(srcdir)/MonoDevelop.WebReferences.NodeBuilders/ProjectNodeBuilder.cs \
	$(srcdir)/MonoDevelop.WebReferences.NodeBuilders/WebReferenceFolderNodeBuilder.cs \
	$(srcdir)/MonoDevelop.WebReferences.NodeBuilders/WebReferenceNodeBuilder.cs \
	$(srcdir)/MonoDevelop.WebReferences/CodeGenerator.cs \
	$(srcdir)/MonoDevelop.WebReferences/DiscoveryNetworkCredential.cs \
	$(srcdir)/MonoDevelop.WebReferences/DiscoveryProtocol.cs \
	$(srcdir)/MonoDevelop.WebReferences/Library.cs \
	$(srcdir)/MonoDevelop.WebReferences/WebReferenceFolder.cs \
	$(srcdir)/MonoDevelop.WebReferences/WebReferenceItem.cs \
	$(srcdir)/MonoDevelop.WebReferences/WebReferenceItemCollection.cs

RES =  \
	$(srcdir)/MonoDevelop.WebReferences.Resources/disco.html \
	$(srcdir)/MonoDevelop.WebReferences.Resources/WebReference.File.Disco.png \
	$(srcdir)/MonoDevelop.WebReferences.Resources/WebReference.File.Wsdl.png \
	$(srcdir)/MonoDevelop.WebReferences.Resources/WebReference.Service.png \
	$(srcdir)/MonoDevelop.WebReferences.Resources/WebReferenceItem.png \
	$(srcdir)/MonoDevelop.WebReferences.Resources/wsdl.html


all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $@

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library \
	$(build_resources:%=/resource:%) $(build_sources) $(REFS)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.WebReferences
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN)
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include
