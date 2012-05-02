DLLNAME = MonoDevelop.AssemblyBrowser.dll
ASSEMBLY = $(top_builddir)/build/AddIns/DisplayBindings/AssemblyBrowser/$(DLLNAME)

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.AssemblyBrowser.AssemblyBrowserWidget.cs \
	MonoDevelop.AssemblyBrowser/AssemblyBrowserHandler.cs \
	MonoDevelop.AssemblyBrowser/AssemblyBrowserViewContent.cs \
	MonoDevelop.AssemblyBrowser/AssemblyBrowserWidget.cs \
	MonoDevelop.AssemblyBrowser/AssemblyLoader.cs \
	MonoDevelop.AssemblyBrowser/AssemblyReferenceFolder.cs \
	MonoDevelop.AssemblyBrowser/AssemblyResourceFolder.cs \
	MonoDevelop.AssemblyBrowser/BaseTypeFolder.cs \
	MonoDevelop.AssemblyBrowser/ColoredCSharpFormatter.cs \
	MonoDevelop.AssemblyBrowser/DisplayBinding.cs \
	MonoDevelop.AssemblyBrowser/DocumentationPanel.cs \
	MonoDevelop.AssemblyBrowser/Error.cs \
	MonoDevelop.AssemblyBrowser/IAssemblyBrowserNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/Namespace.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/AssemblyBrowserTypeNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/AssemblyNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/AssemblyReferenceFolderNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/AssemblyReferenceNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/AssemblyResourceFolderNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/BaseTypeFolderNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/DomEventNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/DomFieldNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/DomMethodNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/DomPropertyNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/DomTypeNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/ErrorNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/ModuleReferenceNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/NamespaceBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/ProjectNodeBuilder.cs \
	MonoDevelop.AssemblyBrowser/TreeNodes/ResourceNodeBuilder.cs \
	XmlDocIdLib/CompatibilityType.cs \
	XmlDocIdLib/OperatorType.cs \
	XmlDocIdLib/XmlDocIdGenerator.cs 

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.AssemblyBrowser.addin.xml

DEPS =  \
	$(top_builddir)/build/AddIns/BackendBindings/MonoDevelop.CSharpBinding.dll \
	$(top_builddir)/build/AddIns/DisplayBindings/SourceEditor/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	$(top_builddir)/build/bin/ICSharpCode.Decompiler.dll \
	$(top_builddir)/build/bin/ICSharpCode.NRefactory.CSharp.dll \
	$(top_builddir)/build/bin/ICSharpCode.NRefactory.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:../../../build/bin/Mono.Cecil.dll \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml

ADDIN_BUILD = $(top_builddir)/build/AddIns/DisplayBindings/AssemblyBrowser

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY) : $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(build_resources:%=/resource:%) $(build_sources) --unsafe -out:$@ -t:library $(REFS) $(build_deps)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

