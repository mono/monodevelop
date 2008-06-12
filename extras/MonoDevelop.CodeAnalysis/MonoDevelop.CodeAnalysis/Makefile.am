ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CodeAnalysis.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	AddinCatalog.cs \
	Argument.cs \
	CodeAnalysis/AssemblyAnalyzerAttribute.cs \
	CodeAnalysis/Category.cs \
	CodeAnalysis/CodeAnalysisException.cs \
	CodeAnalysis/CodeLocation.cs \
	CodeAnalysis/Confidence.cs \
	CodeAnalysis/DictionaryBasedRuleLoader.cs \
	CodeAnalysis/IAnalyzer.cs \
	CodeAnalysis/IRule.cs \
	CodeAnalysis/IRuleLoader.cs \
	CodeAnalysis/IRunner.cs \
	CodeAnalysis/IViolation.cs \
	CodeAnalysis/MainAnalyzer.cs \
	CodeAnalysis/Severity.cs \
	Commands/AnalyzeHandler.cs \
	Commands/CodeAnalysisCommands.cs \
	Extensions/RuleLoaderExtension.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.CodeAnalysis.ProjectOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.CodeAnalysis.ViolationWidget.cs \
	Gui/ProjectOptionsPanel.cs \
	Gui/ProjectOptionsPanelWidget.cs \
	Gui/ResultsReporter.cs \
	Gui/ViolationView.cs \
	Gui/ViolationWidget.cs \
	Project/CodeAnalysisData.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.CodeAnalysis.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.CodeAnalysis
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
