#SUBDIRS = PythonShell

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/BooBinding.dll
BOOSHELL_LIB = $(ADDIN_BUILD)/BooShell.dll

DLLS = -r:System.Drawing \
	-r:System.Xml \
	-r:System.Runtime.Remoting \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	-r:$(top_builddir)/build/bin/ICSharpCode.SharpRefactory.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
	-r:$(BOOSHELL_LIB) \
	$(BOO_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GCONF_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS)

BOOSHELL_DLLS = -r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
		$(GTK_SHARP_LIBS) \
		$(BOO_LIBS)

FILES = \
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

BOOSHELL_FILES = BooShell/BooShell.boo

RES = \
templates/BooGtkSharpProject.xpt.xml \
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

build_sources = $(addprefix $(srcdir)/, $(FILES))
booshell_build_sources = $(addprefix $(srcdir)/, $(BOOSHELL_FILES))

ADDIN = BooBinding.addin.xml

if ENABLE_BOO
all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) \
	$(BOOSHELL_LIB)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(BOOSHELL_LIB) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(BOOC) $(DLLS) $(RES:%=-embedres:%) $(build_sources) -o:$@ -t:library

$(BOOSHELL_LIB): $(BOOSHELL_FILES)
	mkdir -p $(ADDIN_BUILD)
	$(BOOC) $(BOOSHELL_DLLS) $(booshell_build_sources) -o:$@ -t:library

if ENABLE_BOO
assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY) $(ADDIN) $(BOOSHELL_LIB)

endif

CLEANFILES = $(ASSEMBLY) $(BOOSHELL_LIB)
EXTRA_DIST = $(FILES) $(ADDIN) $(RES) $(BOOSHELL_FILES)

