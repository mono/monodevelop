SUBDIRS = . Autotools AspNet

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CSharpBinding.dll

PARSE = parse.exe

DEPS =  \
	$(top_builddir)/build/AddIns/DisplayBindings/SourceEditor/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	$(top_builddir)/build/bin/ICSharpCode.Decompiler.dll \
	$(top_builddir)/build/bin/ICSharpCode.NRefactory.CSharp.dll \
	$(top_builddir)/build/bin/ICSharpCode.NRefactory.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/OldNRefactory.dll


REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:../../../build/bin/Mono.Cecil.dll \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.CodeGeneration.GenerateCodeWindow.cs \
	gtk-gui/MonoDevelop.CSharp.Formatting.CSharpFormattingPolicyPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Formatting.CSharpFormattingProfileDialog.cs \
	gtk-gui/MonoDevelop.CSharp.Formatting.NewFormattingProfileDialog.cs \
	gtk-gui/MonoDevelop.CSharp.Project.CodeGenerationPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Project.CompilerOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Refactoring.CodeIssues.NameConventionEditRuleDialog.cs \
	gtk-gui/MonoDevelop.CSharp.Refactoring.CodeIssues.NameConventionPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Refactoring.ExtractMethod.ExtractMethodDialog.cs \
	MonoDevelop.CSharp.CodeGeneration/AbstractGenerateAction.cs \
	MonoDevelop.CSharp.CodeGeneration/CodeGenerationCommands.cs \
	MonoDevelop.CSharp.CodeGeneration/CodeGenerationOptions.cs \
	MonoDevelop.CSharp.CodeGeneration/CreateConstructorGenerator.cs \
	MonoDevelop.CSharp.CodeGeneration/EqualityMembersGenerator.cs \
	MonoDevelop.CSharp.CodeGeneration/GenerateCodeWindow.cs \
	MonoDevelop.CSharp.CodeGeneration/ICodeGenerator.cs \
	MonoDevelop.CSharp.CodeGeneration/OverrideMembersGenerator.cs \
	MonoDevelop.CSharp.CodeGeneration/PropertyGenerator.cs \
	MonoDevelop.CSharp.CodeGeneration/RaiseEventMethodGenerator.cs \
	MonoDevelop.CSharp.CodeGeneration/ReadonlyPropertyGenerator.cs \
	MonoDevelop.CSharp.CodeGeneration/ToStringGenerator.cs \
	MonoDevelop.CSharp.CodeGeneration/WriteLineGenerator.cs \
	MonoDevelop.CSharp.Completion/CompletionTextLinkMode.cs \
	MonoDevelop.CSharp.Completion/ConstructorParameterDataProvider.cs \
	MonoDevelop.CSharp.Completion/CSharpCompletionTextEditorExtension.cs \
	MonoDevelop.CSharp.Completion/DelegateDataProvider.cs \
	MonoDevelop.CSharp.Completion/EventCreationCompletionData.cs \
	MonoDevelop.CSharp.Completion/IndexerParameterDataProvider.cs \
	MonoDevelop.CSharp.Completion/MemberCompletionData.cs \
	MonoDevelop.CSharp.Completion/MethodParameterDataProvider.cs \
	MonoDevelop.CSharp.Completion/NewOverrideCompletionData.cs \
	MonoDevelop.CSharp.Completion/TemplateParameterDataProvider.cs \
	MonoDevelop.CSharp.Completion/VariableCompletionData.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormatter.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicy.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicyPanel.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicyPanelWidget.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingProfileDialog.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingProperties.cs \
	MonoDevelop.CSharp.Formatting/CSharpIndentEngine.cs \
	MonoDevelop.CSharp.Formatting/CSharpIndentEngineStack.cs \
	MonoDevelop.CSharp.Formatting/CSharpIndentVirtualSpaceManager.cs \
	MonoDevelop.CSharp.Formatting/CSharpTextEditorIndentation.cs \
	MonoDevelop.CSharp.Formatting/Indent.cs \
	MonoDevelop.CSharp.Formatting/NewFormattingProfileDialog.cs \
	MonoDevelop.CSharp.Formatting/OnTheFlyFormatter.cs \
	MonoDevelop.CSharp.Formatting/OnTheFlyFormatterTextEditorExtension.cs \
	MonoDevelop.CSharp.Highlighting/CSharpSyntaxMode.cs \
	MonoDevelop.CSharp.Highlighting/HighlightUsagesExtension.cs \
	MonoDevelop.CSharp.Highlighting/MoveToUsagesHandler.cs \
	MonoDevelop.CSharp.Parser/CSharpFoldingParser.cs \
	MonoDevelop.CSharp.Parser/TypeSystemProvider.cs \
	MonoDevelop.CSharp.Project/CodeGenerationPanel.cs \
	MonoDevelop.CSharp.Project/CompilerOptionsPanelWidget.cs \
	MonoDevelop.CSharp.Project/CSharpCompilerParameters.cs \
	MonoDevelop.CSharp.Project/CSharpProjectParameters.cs \
	MonoDevelop.CSharp.Project/CSharpResourceIdBuilder.cs \
	MonoDevelop.CSharp.Refactoring.CodeActions/Actions/MoveTypeToFile.cs \
	MonoDevelop.CSharp.Refactoring.CodeActions/ContextActionExtensions.cs \
	MonoDevelop.CSharp.Refactoring.CodeActions/MDRefactoringContext.cs \
	MonoDevelop.CSharp.Refactoring.CodeActions/MDRefactoringScript.cs \
	MonoDevelop.CSharp.Refactoring.CodeActions/NRefactoryCodeAction.cs \
	MonoDevelop.CSharp.Refactoring.CodeActions/NRefactoryCodeActionProvider.cs \
	MonoDevelop.CSharp.Refactoring.CodeActions/NRefactoryCodeActionSource.cs \
	MonoDevelop.CSharp.Refactoring.CodeIssues/NamingInspection/NameConventionEditRuleDialog.cs \
	MonoDevelop.CSharp.Refactoring.CodeIssues/NamingInspection/NameConventionPanel.cs \
	MonoDevelop.CSharp.Refactoring.CodeIssues/NamingInspection/NameConventionPanelWidget.cs \
	MonoDevelop.CSharp.Refactoring.CodeIssues/NamingInspection/NameConventionPolicy.cs \
	MonoDevelop.CSharp.Refactoring.CodeIssues/NamingInspection/NameConventionRule.cs \
	MonoDevelop.CSharp.Refactoring.CodeIssues/NRefactoryCodeIssueSource.cs \
	MonoDevelop.CSharp.Refactoring.CodeIssues/NRefactoryIssueProvider.cs \
	MonoDevelop.CSharp.Refactoring/CSharpCodeGenerator.cs \
	MonoDevelop.CSharp.Refactoring/CSharpReferenceFinder.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/ExtractMethodDialog.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/ExtractMethodHandler.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/ExtractMethodRefactoring.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/VariableLookupVisitor.cs \
	MonoDevelop.CSharp.Refactoring/HelperMethods.cs \
	MonoDevelop.CSharp.Resolver/HelperMethods.cs \
	MonoDevelop.CSharp.Resolver/TextEditorResolverProvider.cs \
	MonoDevelop.CSharp.Tooltips/LanguageItemTooltipProvider.cs \
	MonoDevelop.CSharp/CSharpAmbience.cs \
	MonoDevelop.CSharp/CSharpBindingCompilerManager.cs \
	MonoDevelop.CSharp/CSharpEnhancedCodeProvider.cs \
	MonoDevelop.CSharp/CSharpLanguageBinding.cs \
	MonoDevelop.CSharp/ExpandSelectionHandler.cs \
	MonoDevelop.CSharp/PathedDocumentTextEditorExtension.cs

RES =  \
	CSharpBinding.addin.xml \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/C\#.FileIcon \
	icons/csharp-icon-32.png \
	icons/csharp-project-16.png \
	md1format.xml \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicy.xml \
	MonoDevelop.CSharp.Formatting/policies/AllmanCSharpPolicy.xml \
	MonoDevelop.CSharp.Formatting/policies/GNUCSharpPolicy.xml \
	MonoDevelop.CSharp.Formatting/policies/GNUTextStylePolicy.xml \
	MonoDevelop.CSharp.Formatting/policies/InvariantTextStylePolicy.xml \
	MonoDevelop.CSharp.Formatting/policies/KRCSharpPolicy.xml \
	MonoDevelop.CSharp.Formatting/policies/MonoCSharpPolicy.xml \
	MonoDevelop.CSharp.Formatting/policies/SharpDevelopCSharpPolicy.xml \
	MonoDevelop.CSharp.Formatting/policies/WhitesmithsCSharpPolicy.xml \
	MonoDevelop.CSharp.Refactoring.CodeIssues/NamingInspection/MonoNameConventionPolicy.xml \
	templates/AssemblyInfo.xft.xml \
	templates/ConsoleProject.xpt.xml \
	templates/EmptyCSharpFile.xft.xml \
	templates/EmptyProject.xpt.xml \
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
	Project/CSharpCompilerParameters.cs \
	MonoDevelop.CSharp.Inspection/Inspectors/RedundantThisInspector.cs

TESTDLLS = \
	$(NUNIT_LIBS)

TESTASSEMBLY = $(top_builddir)/build/bin/CSharpBinding.Test.dll

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) "-define:GMCS_SOURCE;NET_2_0;MS_COMPATIBLE;FULL_AST;BOOTSTRAP_BASIC" -unsafe  -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

#check: all $(TESTASSEMBLY)
#	MONO_PATH=$(top_builddir)/build/AddIns nunit-console $(TESTASSEMBLY)


$(TESTASSEMBLY) : $(build_test_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -debug "-define:GMCS_SOURCE;NET_2_0;MS_COMPATIBLE;FULL_AST;BOOTSTRAP_BASIC" -unsafe  -codepage:utf8 -out:$@ -target:library $(DLLS) $(TESTDLLS) $(REFS) /r:$(ASSEMBLY) \
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

