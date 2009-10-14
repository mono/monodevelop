SUBDIRS = . Autotools

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CSharpBinding.dll

PARSE = parse.exe

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:../../../build/bin/NRefactory.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.CSharp.Formatting.FormattingPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Project.CodeGenerationPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Project.CompilerOptionsPanelWidget.cs \
	MonoDevelop.CSharp.Completion/CSharpTextEditorCompletion.cs \
	MonoDevelop.CSharp.Completion/EventCreationCompletionData.cs \
	MonoDevelop.CSharp.Completion/MemberCompletionData.cs \
	MonoDevelop.CSharp.Completion/NewOverrideCompletionData.cs \
	MonoDevelop.CSharp.Completion/NRefactoryIndexerParameterDataProvider.cs \
	MonoDevelop.CSharp.Completion/NRefactoryParameterDataProvider.cs \
	MonoDevelop.CSharp.Completion/NRefactoryTemplateParameterDataProvider.cs \
	MonoDevelop.CSharp.Dom/AbstractCSharpNode.cs \
	MonoDevelop.CSharp.Dom/CSharpAmbience.cs \
	MonoDevelop.CSharp.Dom/Expressions/AnonymousMethodExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/AssignmentExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/BaseReferenceExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/BinaryOperatorExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/CastExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/CheckedExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/ConditionalExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/IdentifierExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/IndexerExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/InvocationExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/MemberReferenceExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/NullReferenceExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/ObjectCreateExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/ParenthesizedExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/PointerReferenceExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/PrimitiveExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/QueryExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/SizeOfExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/StackAllocExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/ThisReferenceExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/TypeOfExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/UnaryOperatorExpression.cs \
	MonoDevelop.CSharp.Dom/Expressions/UncheckedExpression.cs \
	MonoDevelop.CSharp.Dom/GeneralScope/Attribute.cs \
	MonoDevelop.CSharp.Dom/GeneralScope/AttributeSection.cs \
	MonoDevelop.CSharp.Dom/GeneralScope/DelegateDeclaration.cs \
	MonoDevelop.CSharp.Dom/GeneralScope/EnumDeclaration.cs \
	MonoDevelop.CSharp.Dom/GeneralScope/NamespaceDeclaration.cs \
	MonoDevelop.CSharp.Dom/GeneralScope/TypeDeclaration.cs \
	MonoDevelop.CSharp.Dom/GeneralScope/UsingAliasDeclaration.cs \
	MonoDevelop.CSharp.Dom/GeneralScope/UsingDeclaration.cs \
	MonoDevelop.CSharp.Dom/ICSharpDomVisitor.cs \
	MonoDevelop.CSharp.Dom/ICSharpNode.cs \
	MonoDevelop.CSharp.Dom/Identifier.cs \
	MonoDevelop.CSharp.Dom/QualifiedIdentifier.cs \
	MonoDevelop.CSharp.Dom/Statements/BlockStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/BreakStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/CheckedStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/ContinueStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/EmptyStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/FixedStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/ForeachStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/ForStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/GotoStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/IfElseStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/LabelStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/LockStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/ReturnStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/SwitchStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/ThrowStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/TryCatchStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/UncheckedStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/UnsafeStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/UsingStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/VariableDeclarationStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/WhileStatement.cs \
	MonoDevelop.CSharp.Dom/Statements/YieldStatement.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/AbstractMember.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/AbstractMemberBase.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/ArgumentDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/ConstructorDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/DestructorDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/EventDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/FieldDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/IndexerDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/MethodDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/OperatorDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/PropertyDeclaration.cs \
	MonoDevelop.CSharp.Dom/TypeMembers/VariableInitializer.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormatter.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicy.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicyPanel.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingProperties.cs \
	MonoDevelop.CSharp.Formatting/CSharpIndentEngine.cs \
	MonoDevelop.CSharp.Formatting/CSharpIndentEngineStack.cs \
	MonoDevelop.CSharp.Formatting/CSharpTextEditorIndentation.cs \
	MonoDevelop.CSharp.Formatting/FormattingPanel.cs \
	MonoDevelop.CSharp.Highlighting/CSharpSyntaxMode.cs \
	MonoDevelop.CSharp.Highlighting/HighlightCSharpSemanticRule.cs \
	MonoDevelop.CSharp.Parser/CSharpParser.cs \
	MonoDevelop.CSharp.Parser/NRefactoryParser.cs \
	MonoDevelop.CSharp.Project/CodeGenerationPanel.cs \
	MonoDevelop.CSharp.Project/CompilerOptionsPanelWidget.cs \
	MonoDevelop.CSharp.Project/CSharpCompilerParameters.cs \
	MonoDevelop.CSharp.Project/CSharpProjectParameters.cs \
	MonoDevelop.CSharp.Project/CSharpResourceIdBuilder.cs \
	MonoDevelop.CSharp.Refactoring/CodeGenerator.cs \
	MonoDevelop.CSharp.Refactoring/CSharpNRefactoryASTProvider.cs \
	MonoDevelop.CSharp.Refactoring/FindMemberAstVisitor.cs \
	MonoDevelop.CSharp.Resolver/NewCSharpExpressionFinder.cs \
	MonoDevelop.CSharp.Resolver/NRefactoryDocumentMetaInformation.cs \
	MonoDevelop.CSharp.Resolver/NRefactoryResolver.cs \
	MonoDevelop.CSharp.Resolver/ResolveVisitor.cs \
	MonoDevelop.CSharp/CSharpBindingCompilerManager.cs \
	MonoDevelop.CSharp/CSharpEnhancedCodeProvider.cs \
	MonoDevelop.CSharp/CSharpLanguageBinding.cs

RES =  \
	CSharpBinding.addin.xml \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/C\#.FileIcon \
	icons/csharp-icon-32.png \
	icons/csharp-project-16.png \
	md1format.xml \
	MonoCSharpPolicy.xml \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicy.xml \
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

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -define:GMCS_SOURCE -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

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

