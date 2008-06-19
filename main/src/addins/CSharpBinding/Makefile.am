SUBDIRS = . Autotools

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CSharpBinding.dll

PARSE = parse.exe

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/NRefactory.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	CSharpAmbience.cs \
	CSharpBindingCompilerManager.cs \
	CSharpEnhancedCodeProvider.cs \
	CSharpLanguageBinding.cs \
	FormattingStrategy/CSharpFormattingProperties.cs \
	FormattingStrategy/CSharpIndentEngine.cs \
	FormattingStrategy/CSharpIndentEngineStack.cs \
	gtk-gui/CSharpBinding.CodeGenerationPanelWidget.cs \
	gtk-gui/CSharpBinding.CompilerOptionsPanelWidget.cs \
	gtk-gui/CSharpBinding.FormattingPanelWidget.cs \
	gtk-gui/generated.cs \
	Gui/CodeGenerationPanel.cs \
	Gui/CompilerOptionsPanelWidget.cs \
	Gui/CSharpParameterDataProvider.cs \
	Gui/CSharpTextEditorCompletion.cs \
	Gui/CSharpTextEditorExtension.cs \
	Gui/FormattingPanel.cs \
	Gui/NRefactoryResolver.cs \
	Gui/OverrideCompletionData.cs \
	Parser/CodeGenerator.cs \
	Parser/CSharpExpressionFinder.cs \
	Parser/CSharpVisitor.cs \
	Parser/DomParser.cs \
	Parser/ExpressionFinder.cs \
	Parser/LanguageItemVisitor.cs \
	Parser/mcs/cs-parser.cs \
	Parser/mcs/dom.cs \
	Parser/mcs/External/CryptoConvert.cs \
	Parser/mcs/External/MonoSymbolFile.cs \
	Parser/mcs/External/MonoSymbolTable.cs \
	Parser/mcs/External/MonoSymbolWriter.cs \
	Parser/mcs/mcs/anonymous.cs \
	Parser/mcs/mcs/assign.cs \
	Parser/mcs/mcs/attribute.cs \
	Parser/mcs/mcs/cfold.cs \
	Parser/mcs/mcs/class.cs \
	Parser/mcs/mcs/codegen.cs \
	Parser/mcs/mcs/const.cs \
	Parser/mcs/mcs/constant.cs \
	Parser/mcs/mcs/convert.cs \
	Parser/mcs/mcs/cs-tokenizer.cs \
	Parser/mcs/mcs/decl.cs \
	Parser/mcs/mcs/delegate.cs \
	Parser/mcs/mcs/doc.cs \
	Parser/mcs/mcs/driver.cs \
	Parser/mcs/mcs/ecore.cs \
	Parser/mcs/mcs/enum.cs \
	Parser/mcs/mcs/expression.cs \
	Parser/mcs/mcs/flowanalysis.cs \
	Parser/mcs/mcs/generic.cs \
	Parser/mcs/mcs/iterators.cs \
	Parser/mcs/mcs/lambda.cs \
	Parser/mcs/mcs/linq.cs \
	Parser/mcs/mcs/literal.cs \
	Parser/mcs/mcs/location.cs \
	Parser/mcs/mcs/modifiers.cs \
	Parser/mcs/mcs/namespace.cs \
	Parser/mcs/mcs/nullable.cs \
	Parser/mcs/mcs/parameter.cs \
	Parser/mcs/mcs/pending.cs \
	Parser/mcs/mcs/report.cs \
	Parser/mcs/mcs/rootcontext.cs \
	Parser/mcs/mcs/roottypes.cs \
	Parser/mcs/mcs/statement.cs \
	Parser/mcs/mcs/support.cs \
	Parser/mcs/mcs/symbolwriter.cs \
	Parser/mcs/mcs/typemanager.cs \
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
	Project/CSharpCompilerParameters.cs \
	Project/CSharpResourceIdBuilder.cs

RES =  \
	CSharpBinding.addin.xml \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/C\#.FileIcon \
	icons/csharp-icon-32.png \
	md1format.xml \
	templates/AssemblyInfo.xft.xml \
	templates/ConsoleProject.xpt.xml \
	templates/EmptyCSharpFile.xft.xml \
	templates/EmptyProject.xpt.xml \
	templates/GladeSharp2Project.xpt.xml \
	templates/GnomeSharp2Project.xpt.xml \
	templates/GtkSharp2Project.xpt.xml \
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
	$(CSC) $(CSC_FLAGS) -define:GMCS_SOURCE -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

#check: all $(TESTASSEMBLY)
#	MONO_PATH=$(top_builddir)/build/AddIns nunit-console $(TESTASSEMBLY)


$(TESTASSEMBLY) : $(build_test_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -debug -define:GMCS_SOURCE -codepage:utf8 -out:$@ -target:library $(DLLS) $(TESTDLLS) $(REFS) /r:$(ASSEMBLY) \
	$(monodoc_libflags) $(build_resources:%=/resource:%) $(build_test_sources)

parse: $(PARSE)

$(PARSE): parse.cs $(ASSEMBLY)
	$(CSC) -out:$@ parse.cs $(REFS) -r:$(ASSEMBLY)

run-parse-test: $(PARSE)
	MONO_PATH=$(top_builddir)/build/AddIns/BackendBindings/:$(top_builddir)/build/bin/ mono $(PARSE) .

csharpbindinglibdir = $(MD_ADDIN_DIR)/BackendBindings
csharpbindinglib_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(PARSE) $(PARSE).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

