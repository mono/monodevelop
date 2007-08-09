SUBDIRS = . Autotools

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/CSharpBinding.dll

PARSE = parse.exe

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:../../build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:../../build/bin/MonoDevelop.Components.dll \
	-r:../../build/bin/MonoDevelop.Core.dll \
	-r:../../build/bin/MonoDevelop.Core.Gui.dll \
	-r:../../build/bin/MonoDevelop.Ide.dll \
	-r:../../build/bin/MonoDevelop.Projects.dll \
	-r:../../build/bin/MonoDevelop.Projects.Gui.dll \
	-r:../../build/bin/NRefactory.dll \
	-r:../../contrib/Mono.Addins.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES = \
Gui/FormattingPanel.cs \
Gui/ChooseRuntimePanel.cs \
Gui/CodeGenerationPanel.cs \
Gui/CSharpTextEditorExtension.cs \
Gui/CSharpParameterDataProvider.cs \
Gui/OverrideCompletionData.cs \
Parser/SharpDevelopTree/Destructor.cs \
Parser/SharpDevelopTree/Constructor.cs \
Parser/SharpDevelopTree/Method.cs \
Parser/SharpDevelopTree/Class.cs \
Parser/SharpDevelopTree/AttributeSection.cs \
Parser/SharpDevelopTree/ReturnType.cs \
Parser/SharpDevelopTree/GenericParameter.cs \
Parser/TypeVisitor.cs \
Parser/CSharpVisitor.cs \
Parser/LanguageItemVisitor.cs \
Parser/Resolver.cs \
Parser/Parser.cs \
Parser/CodeGenerator.cs \
Parser/ExpressionFinder.cs \
CSharpAmbience.cs \
Project/CSharpCompilerParameters.cs \
CSharpLanguageBinding.cs \
FormattingStrategy/CSharpFormattingProperties.cs \
FormattingStrategy/CSharpFormattingStrategy.cs \
FormattingStrategy/CSharpIndentEngineStack.cs \
FormattingStrategy/CSharpIndentEngine.cs \
CSharpBindingCompilerManager.cs \
CSharpEnhancedCodeProvider.cs \
gtk-gui/generated.cs \
gtk-gui/CSharpBinding.FormattingPanelWidget.cs \
gtk-gui/CSharpBinding.CodeGenerationPanelWidget.cs

RES =  \
	CSharpBinding.addin.xml \
	gtk-gui/gui.stetic \
	icons/C\#.File.EmptyFile \
	icons/C\#.File.Form \
	icons/C\#.File.FullFile \
	icons/C\#.File.NewClass \
	icons/C\#.File.WebFile \
	icons/C\#.FileIcon \
	icons/C\#.Project.DOSProject \
	icons/C\#.Project.EmptyProject \
	icons/C\#.Project.Form \
	icons/C\#.Project.FullProject \
	icons/C\#.Project.Library \
	icons/C\#.Project.ServiceProject \
	icons/C\#.Project.UserControl \
	icons/C\#.Project.WebProject \
	icons/C\#.ProjectIcon \
	templates/ConsoleProject.xpt.xml \
	templates/EmptyCSharpFile.xft.xml \
	templates/EmptyProject.xpt.xml \
	templates/GladeSharp2Project.xpt.xml \
	templates/GladeSharpProject.xpt.xml \
	templates/GnomeSharp2Project.xpt.xml \
	templates/GnomeSharpProgram.xft.xml \
	templates/GnomeSharpProject.xpt.xml \
	templates/GtkSharp2Project.xpt.xml \
	templates/GtkSharpProject.xpt.xml \
	templates/GtkSharpWindow.xft.xml \
	templates/Library.xpt.xml

TESTFILES =  \
	CSharpAmbience.cs \
	CSharpBindingCompilerManager.cs \
	CSharpEnhancedCodeProvider.cs \
	CSharpLanguageBinding.cs \
	FormattingStrategy/CSharpFormattingProperties.cs \
	FormattingStrategy/CSharpFormattingStrategy.cs \
	FormattingStrategy/CSharpIndentEngine.cs \
	FormattingStrategy/CSharpIndentEngineStack.cs \
	gtk-gui/CSharpBinding.CodeGenerationPanelWidget.cs \
	gtk-gui/CSharpBinding.FormattingPanelWidget.cs \
	gtk-gui/generated.cs \
	Gui/ChooseRuntimePanel.cs \
	Gui/CodeGenerationPanel.cs \
	Gui/CSharpParameterDataProvider.cs \
	Gui/CSharpTextEditorExtension.cs \
	Gui/FormattingPanel.cs \
	Gui/OverrideCompletionData.cs \
	Parser/CodeGenerator.cs \
	Parser/CSharpVisitor.cs \
	Parser/ExpressionFinder.cs \
	Parser/LanguageItemVisitor.cs \
	Parser/Parser.cs \
	Parser/Resolver.cs \
	Parser/SharpDevelopTree/AttributeSection.cs \
	Parser/SharpDevelopTree/Class.cs \
	Parser/SharpDevelopTree/Constructor.cs \
	Parser/SharpDevelopTree/Destructor.cs \
	Parser/SharpDevelopTree/GenericParameter.cs \
	Parser/SharpDevelopTree/Method.cs \
	Parser/SharpDevelopTree/ReturnType.cs \
	Parser/TypeVisitor.cs \
	Project/CSharpCompilerParameters.cs

TESTDLLS = \
	$(NUNIT_LIBS)

TESTASSEMBLY = $(top_builddir)/build/bin/CSharpBinding.Test.dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

check: all $(TESTASSEMBLY)	
	MONO_PATH=../../build/AddIns nunit-console $(TESTASSEMBLY)

$(TESTASSEMBLY) : $(build_test_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -debug -codepage:utf8 -out:$@ -target:library $(DLLS) $(TESTDLLS) /r:$(ASSEMBLY) \
	$(monodoc_libflags) $(build_resources:%=/resource:%) $(build_test_sources)

parse: $(PARSE)

$(PARSE): parse.cs $(ASSEMBLY)
	$(CSC) -out:$@ parse.cs $(REFS) -r:$(ASSEMBLY)

run-parse-test: $(PARSE)
	MONO_PATH=../../../../../build/AddIns/BackendBindings/:../../../../../build/bin/ mono $(PARSE) .

csharpbindinglibdir = $(MD_ADDIN_DIR)/BackendBindings
csharpbindinglib_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(PARSE) $(PARSE).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

