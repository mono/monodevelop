
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.WebReferences
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.WebReferences.dll

REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Web \
	-r:System.Web.Services \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.WebReferences.Dialogs.WebReferenceDialog.cs \
	MonoDevelop.WebReferences.Commands/WebReferenceCommandHandler.cs \
	MonoDevelop.WebReferences.Commands/WebReferenceCommands.cs \
	MonoDevelop.WebReferences.Dialogs/WebReferenceDialog.cs \
	MonoDevelop.WebReferences.NodeBuilders/ProjectFolderNodeBuilderExtension.cs \
	MonoDevelop.WebReferences.NodeBuilders/ProjectNodeBuilder.cs \
	MonoDevelop.WebReferences.NodeBuilders/WebReferenceFolderNodeBuilder.cs \
	MonoDevelop.WebReferences.NodeBuilders/WebReferenceNodeBuilder.cs \
	MonoDevelop.WebReferences/CodeGenerator.cs \
	MonoDevelop.WebReferences/DiscoveryNetworkCredential.cs \
	MonoDevelop.WebReferences/DiscoveryProtocol.cs \
	MonoDevelop.WebReferences/Library.cs \
	MonoDevelop.WebReferences/WebReferenceFolder.cs \
	MonoDevelop.WebReferences/WebReferenceItem.cs \
	MonoDevelop.WebReferences/WebReferenceItemCollection.cs

RES =  \
	MonoDevelop.WebReferences.addin.xml \
	MonoDevelop.WebReferences.Resources/disco.html \
	MonoDevelop.WebReferences.Resources/WebReference.File.Disco.png \
	MonoDevelop.WebReferences.Resources/WebReference.File.Wsdl.png \
	MonoDevelop.WebReferences.Resources/WebReference.Service.png \
	MonoDevelop.WebReferences.Resources/WebReferenceItem.png \
	MonoDevelop.WebReferences.Resources/wsdl.html

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library \
	$(build_resources:%=/resource:%) $(build_sources) $(REFS)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.WebReferences
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
