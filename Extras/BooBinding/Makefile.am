SUBDIRS = BooShell

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/BooBinding.dll

REFS =	-r:System.Drawing \
	-r:System.Xml \
	-r:System.Runtime.Remoting \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:$(top_builddir)/build/AddIns/ICSharpCode.SharpRefactory.dll \
	-r:$(top_builddir)/build/AddIns/BackendBindings/BooShell.dll \
	$(BOO_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GCONF_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS)

FILES = \
FormattingStrategy/BooFormattingStrategy.boo \
Gui/ShellTextView.boo \
Gui/IShellModel.boo \
Gui/BooShellModel.boo \
Gui/OptionPanels/CodeCompilationPanel.boo \
Gui/OptionPanels/GeneralShellPanel.boo \
Gui/OptionPanels/GeneralBooShellPanel.boo \
Project/BooCompilerParameters.boo \
Properties/ShellProperties.boo \
Properties/BooShellProperties.boo \
BooBindingCompilerServices.boo \
BooAmbience.boo \
BooShellPadContent.boo \
BooCompiler.boo \
BooLanguageBinding.boo \
Parser/BooParser.boo \
Parser/Resolver.boo \
Parser/TypeMembers.boo \
Parser/ExpressionFinder.boo \
Parser/ReturnType.boo \
Parser/VariableLookupVisitor.boo \
Parser/ExpressionTypeVisitor.boo \
Parser/Tree.boo \
Parser/Visitor.boo

RES = \
templates/BooGtkSharpProject.xpt.xml \
templates/BooLibraryProject.xpt.xml \
templates/BooGtkSharpWindow.xft.xml \
templates/EmptyBooFile.xft.xml \
templates/EmptyBooProject.xpt.xml \
icons/BooBinding.Base \
icons/Boo.File.EmptyFile \
icons/Boo.File.Form \
icons/Boo.FileIcon \
icons/Boo.Project.EmptyProject \
icons/Boo.Project.Form \
icons/Boo.ProjectIcon

ADDIN = BooBinding.addin.xml

if ENABLE_BOO
all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(BOOC) $(REFS) $(RES:%=-embedres:%) $(build_sources) -o:$@ -t:library

if ENABLE_BOO
assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY) $(ADDIN)

endif

CLEANFILES = $(ASSEMBLY)
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)
include $(top_srcdir)/Makefile.include
