DLLNAME = MonoDevelop.AssemblyBrowser.dll
ASSEMBLY = $(top_builddir)/build/AddIns/$(DLLNAME)

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.AssemblyBrowser.AssemblyBrowserWidget.cs \
	MonoDevelop.AssemblyBrowser/AssemblyBrowserView.cs \
	MonoDevelop.AssemblyBrowser/AssemblyBrowserWidget.cs \
	MonoDevelop.AssemblyBrowser/AssemblyNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/BaseTypeFolder.cs \
	MonoDevelop.AssemblyBrowser/BaseTypeFolderNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/Decompiler.cs \
	MonoDevelop.AssemblyBrowser/DisplayBinding.cs \
	MonoDevelop.AssemblyBrowser/DomEventNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/DomFieldNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/DomMethodNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/DomPropertyNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/DomReturnTypeNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/DomTypeNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/Error.cs \
	MonoDevelop.AssemblyBrowser/ErrorNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/IAssemblyBrowserNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/ModuleDefinitionNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/ModuleReferenceNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/Namespace.cs \
	MonoDevelop.AssemblyBrowser/NamespaceBuilder.cs \
	MonoDevelop.AssemblyBrowser/Reference.cs \
	MonoDevelop.AssemblyBrowser/ReferenceFolder.cs \
	MonoDevelop.AssemblyBrowser/ReferenceFolderNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/ReferenceNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/ResourceFolder.cs \
	MonoDevelop.AssemblyBrowser/ResourceFolderNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/ResourceNodeBuilder.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.AssemblyBrowser.addin.xml

GENERATED_FILES =  

DEPS =  \
	$(top_builddir)/build/bin/Mono.Cecil.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

  all: $(ASSEMBLY)

$(ASSEMBLY) : $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(top_builddir)/build/bin
	$(CSC) $(CSC_FLAGS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -t:library $(REFS) $(build_deps)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

