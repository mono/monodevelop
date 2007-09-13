SUBDIRS = BooShell

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/BooBinding.dll

REFS =  \
	$(BOO_LIBS) \
	$(GCONF_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/BackendBindings/BooShell.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.Setup.dll \
	-r:$(top_builddir)/build/bin/Mono.Cecil.dll \
	-r:System.Drawing \
	-r:System.Runtime.Remoting \
	-r:System.Xml

FILES =  \
	BooAmbience.boo \
	BooBindingCompilerServices.boo \
	BooCompiler.boo \
	BooLanguageBinding.boo \
	BooShellPadContent.boo \
	FormattingStrategy/BooFormattingStrategy.boo \
	Gui/BooShellModel.boo \
	Gui/BooTextEditorExtension.boo \
	Gui/IShellModel.boo \
	Gui/OptionPanels/CodeCompilationPanel.boo \
	Gui/OptionPanels/GeneralBooShellPanel.boo \
	Gui/OptionPanels/GeneralShellPanel.boo \
	Gui/ShellTextView.boo \
	Parser/BooParser.boo \
	Parser/ExpressionFinder.boo \
	Parser/ExpressionTypeVisitor.boo \
	Parser/Resolver.boo \
	Parser/ReturnType.boo \
	Parser/Tree.boo \
	Parser/TypeMembers.boo \
	Parser/VariableLookupVisitor.boo \
	Parser/Visitor.boo \
	Project/BooCompilerParameters.boo \
	Properties/BooShellProperties.boo \
	Properties/ShellProperties.boo

RES =  \
	BooBinding.addin.xml \
	icons/Boo.File.EmptyFile \
	icons/Boo.File.Form \
	icons/Boo.FileIcon \
	icons/Boo.Project.EmptyProject \
	icons/Boo.Project.Form \
	icons/Boo.ProjectIcon \
	icons/BooBinding.Base \
	templates/BooGtkSharpProject.xpt.xml \
	templates/BooGtkSharpWindow.xft.xml \
	templates/BooLibraryProject.xpt.xml \
	templates/EmptyBooFile.xft.xml \
	templates/EmptyBooProject.xpt.xml

if ENABLE_BOO
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(BOOC) $(REFS) $(RES:%=-embedres:%) $(build_sources) -o:$@ -t:library

if ENABLE_BOO
assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY)

endif

CLEANFILES = $(ASSEMBLY)
EXTRA_DIST = $(FILES) $(RES)
include $(top_srcdir)/Makefile.include
