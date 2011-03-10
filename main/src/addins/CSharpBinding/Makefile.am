SUBDIRS = . Autotools AspNet

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CSharpBinding.dll

PARSE = parse.exe

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/NRefactory.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.CSharp.Formatting.CSharpFormattingPolicyPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Formatting.CSharpFormattingProfileDialog.cs \
	gtk-gui/MonoDevelop.CSharp.Formatting.NewFormattingProfileDialog.cs \
	gtk-gui/MonoDevelop.CSharp.Project.CodeGenerationPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Project.CompilerOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.CSharp.Refactoring.ExtractMethod.ExtractMethodDialog.cs \
	MonoDevelop.CSharp.Ast/AstLocation.cs \
	MonoDevelop.CSharp.Ast/AstNode.cs \
	MonoDevelop.CSharp.Ast/AstType.cs \
	MonoDevelop.CSharp.Ast/CompilationUnit.cs \
	MonoDevelop.CSharp.Ast/ComposedType.cs \
	MonoDevelop.CSharp.Ast/CSharpAmbience.cs \
	MonoDevelop.CSharp.Ast/CSharpModifierToken.cs \
	MonoDevelop.CSharp.Ast/CSharpTokenNode.cs \
	MonoDevelop.CSharp.Ast/DepthFirstAstVisitor.cs \
	MonoDevelop.CSharp.Ast/Expressions/AnonymousMethodExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/ArgListExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/ArrayCreateExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/ArrayInitializerExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/AsExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/AssignmentExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/BaseReferenceExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/BinaryOperatorExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/CastExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/CheckedExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/ConditionalExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/DefaultValueExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/DirectionExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/EmptyExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/Expression.cs \
	MonoDevelop.CSharp.Ast/Expressions/IdentifierExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/IndexerExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/InvocationExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/IsExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/LambdaExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/MemberReferenceExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/NamedArgumentExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/NullReferenceExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/ObjectCreateExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/ParenthesizedExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/PointerReferenceExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/PrimitiveExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/QueryExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/SizeOfExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/StackAllocExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/ThisReferenceExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/TypeOfExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/UnaryOperatorExpression.cs \
	MonoDevelop.CSharp.Ast/Expressions/UncheckedExpression.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/Attribute.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/AttributeSection.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/Comment.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/Constraint.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/DelegateDeclaration.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/NamespaceDeclaration.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/TypeDeclaration.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/TypeParameterDeclaration.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/UsingAliasDeclaration.cs \
	MonoDevelop.CSharp.Ast/GeneralScope/UsingDeclaration.cs \
	MonoDevelop.CSharp.Ast/IAstVisitor.cs \
	MonoDevelop.CSharp.Ast/Identifier.cs \
	MonoDevelop.CSharp.Ast/LookupTableVisitor.cs \
	MonoDevelop.CSharp.Ast/MemberType.cs \
	MonoDevelop.CSharp.Ast/NodeType.cs \
	MonoDevelop.CSharp.Ast/PrimitiveType.cs \
	MonoDevelop.CSharp.Ast/QualifiedIdentifier.cs \
	MonoDevelop.CSharp.Ast/Role.cs \
	MonoDevelop.CSharp.Ast/SimpleType.cs \
	MonoDevelop.CSharp.Ast/Statements/BlockStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/BreakStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/CheckedStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/ContinueStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/DoWhileStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/EmptyStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/ExpressionStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/FixedStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/ForeachStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/ForStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/GotoStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/IfElseStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/LabelStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/LockStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/ReturnStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/Statement.cs \
	MonoDevelop.CSharp.Ast/Statements/SwitchStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/ThrowStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/TryCatchStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/UncheckedStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/UnsafeStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/UsingStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/VariableDeclarationStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/WhileStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/YieldBreakStatement.cs \
	MonoDevelop.CSharp.Ast/Statements/YieldStatement.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/Accessor.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/AttributedNode.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/ConstructorDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/DestructorDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/EnumMemberDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/EventDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/FieldDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/IndexerDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/MemberDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/MethodDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/OperatorDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/ParameterDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/PropertyDeclaration.cs \
	MonoDevelop.CSharp.Ast/TypeMembers/VariableInitializer.cs \
	MonoDevelop.CSharp.Ast/Utils/ModiferNodeExtension.cs \
	MonoDevelop.CSharp.Ast/Utils/TreeTraversal.cs \
	MonoDevelop.CSharp.Completion/CompletionTextLinkMode.cs \
	MonoDevelop.CSharp.Completion/CSharpTextEditorCompletion.cs \
	MonoDevelop.CSharp.Completion/EventCreationCompletionData.cs \
	MonoDevelop.CSharp.Completion/MemberCompletionData.cs \
	MonoDevelop.CSharp.Completion/NewOverrideCompletionData.cs \
	MonoDevelop.CSharp.Completion/NRefactoryIndexerParameterDataProvider.cs \
	MonoDevelop.CSharp.Completion/NRefactoryParameterDataProvider.cs \
	MonoDevelop.CSharp.Completion/NRefactoryTemplateParameterDataProvider.cs \
	MonoDevelop.CSharp.Formatting/AstIndentationVisitor.cs \
	MonoDevelop.CSharp.Formatting/AstSpacingVisitor.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormatter.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicy.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicyPanel.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingPolicyPanelWidget.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingProfileDialog.cs \
	MonoDevelop.CSharp.Formatting/CSharpFormattingProperties.cs \
	MonoDevelop.CSharp.Formatting/CSharpIndentEngine.cs \
	MonoDevelop.CSharp.Formatting/CSharpIndentEngineStack.cs \
	MonoDevelop.CSharp.Formatting/CSharpTextEditorIndentation.cs \
	MonoDevelop.CSharp.Formatting/Indent.cs \
	MonoDevelop.CSharp.Formatting/NewFormattingProfileDialog.cs \
	MonoDevelop.CSharp.Formatting/OnTheFlyFormatter.cs \
	MonoDevelop.CSharp.Highlighting/CSharpSyntaxMode.cs \
	MonoDevelop.CSharp.Highlighting/HighlightCSharpSemanticRule.cs \
	MonoDevelop.CSharp.Highlighting/HighlightUsagesExtension.cs \
	MonoDevelop.CSharp.Highlighting/MoveToUsagesHandler.cs \
	MonoDevelop.CSharp.Parser/CSharpParser.cs \
	MonoDevelop.CSharp.Parser/mcs/anonymous.cs \
	MonoDevelop.CSharp.Parser/mcs/argument.cs \
	MonoDevelop.CSharp.Parser/mcs/assembly.cs \
	MonoDevelop.CSharp.Parser/mcs/assign.cs \
	MonoDevelop.CSharp.Parser/mcs/attribute.cs \
	MonoDevelop.CSharp.Parser/mcs/cfold.cs \
	MonoDevelop.CSharp.Parser/mcs/class.cs \
	MonoDevelop.CSharp.Parser/mcs/codegen.cs \
	MonoDevelop.CSharp.Parser/mcs/complete.cs \
	MonoDevelop.CSharp.Parser/mcs/const.cs \
	MonoDevelop.CSharp.Parser/mcs/constant.cs \
	MonoDevelop.CSharp.Parser/mcs/context.cs \
	MonoDevelop.CSharp.Parser/mcs/convert.cs \
	MonoDevelop.CSharp.Parser/mcs/CryptoConvert.cs \
	MonoDevelop.CSharp.Parser/mcs/cs-parser.cs \
	MonoDevelop.CSharp.Parser/mcs/cs-tokenizer.cs \
	MonoDevelop.CSharp.Parser/mcs/decl.cs \
	MonoDevelop.CSharp.Parser/mcs/delegate.cs \
	MonoDevelop.CSharp.Parser/mcs/doc.cs \
	MonoDevelop.CSharp.Parser/mcs/driver.cs \
	MonoDevelop.CSharp.Parser/mcs/dynamic.cs \
	MonoDevelop.CSharp.Parser/mcs/ecore.cs \
	MonoDevelop.CSharp.Parser/mcs/enum.cs \
	MonoDevelop.CSharp.Parser/mcs/eval.cs \
	MonoDevelop.CSharp.Parser/mcs/expression.cs \
	MonoDevelop.CSharp.Parser/mcs/field.cs \
	MonoDevelop.CSharp.Parser/mcs/flowanalysis.cs \
	MonoDevelop.CSharp.Parser/mcs/generic.cs \
	MonoDevelop.CSharp.Parser/mcs/import.cs \
	MonoDevelop.CSharp.Parser/mcs/iterators.cs \
	MonoDevelop.CSharp.Parser/mcs/lambda.cs \
	MonoDevelop.CSharp.Parser/mcs/linq.cs \
	MonoDevelop.CSharp.Parser/mcs/literal.cs \
	MonoDevelop.CSharp.Parser/mcs/location.cs \
	MonoDevelop.CSharp.Parser/mcs/membercache.cs \
	MonoDevelop.CSharp.Parser/mcs/method.cs \
	MonoDevelop.CSharp.Parser/mcs/modifiers.cs \
	MonoDevelop.CSharp.Parser/mcs/MonoSymbolFile.cs \
	MonoDevelop.CSharp.Parser/mcs/MonoSymbolTable.cs \
	MonoDevelop.CSharp.Parser/mcs/MonoSymbolWriter.cs \
	MonoDevelop.CSharp.Parser/mcs/namespace.cs \
	MonoDevelop.CSharp.Parser/mcs/nullable.cs \
	MonoDevelop.CSharp.Parser/mcs/outline.cs \
	MonoDevelop.CSharp.Parser/mcs/parameter.cs \
	MonoDevelop.CSharp.Parser/mcs/pending.cs \
	MonoDevelop.CSharp.Parser/mcs/property.cs \
	MonoDevelop.CSharp.Parser/mcs/reflection.cs \
	MonoDevelop.CSharp.Parser/mcs/report.cs \
	MonoDevelop.CSharp.Parser/mcs/rootcontext.cs \
	MonoDevelop.CSharp.Parser/mcs/roottypes.cs \
	MonoDevelop.CSharp.Parser/mcs/statement.cs \
	MonoDevelop.CSharp.Parser/mcs/support.cs \
	MonoDevelop.CSharp.Parser/mcs/symbolwriter.cs \
	MonoDevelop.CSharp.Parser/mcs/typemanager.cs \
	MonoDevelop.CSharp.Parser/mcs/typespec.cs \
	MonoDevelop.CSharp.Parser/mcs/visit.cs \
	MonoDevelop.CSharp.Parser/McsParser.cs \
	MonoDevelop.CSharp.Project/CodeGenerationPanel.cs \
	MonoDevelop.CSharp.Project/CompilerOptionsPanelWidget.cs \
	MonoDevelop.CSharp.Project/CSharpCompilerParameters.cs \
	MonoDevelop.CSharp.Project/CSharpProjectParameters.cs \
	MonoDevelop.CSharp.Project/CSharpResourceIdBuilder.cs \
	MonoDevelop.CSharp.Refactoring/CreateMethod/CreateMethodCodeGenerator.cs \
	MonoDevelop.CSharp.Refactoring/CreateMethod/CreateMethodHandler.cs \
	MonoDevelop.CSharp.Refactoring/CSharpCodeGenerator.cs \
	MonoDevelop.CSharp.Refactoring/CSharpNRefactoryASTProvider.cs \
	MonoDevelop.CSharp.Refactoring/CSharpRefactorer.cs \
	MonoDevelop.CSharp.Refactoring/CSharpReferenceFinder.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/ExtractMethodAstTransformer.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/ExtractMethodDialog.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/ExtractMethodHandler.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/ExtractMethodRefactoring.cs \
	MonoDevelop.CSharp.Refactoring/ExtractMethod/VariableLookupVisitor.cs \
	MonoDevelop.CSharp.Refactoring/FindMemberAstVisitor.cs \
	MonoDevelop.CSharp.Resolver/NewCSharpExpressionFinder.cs \
	MonoDevelop.CSharp.Resolver/NRefactoryDocumentMetaInformation.cs \
	MonoDevelop.CSharp.Resolver/NRefactoryResolver.cs \
	MonoDevelop.CSharp.Resolver/ResolveVisitor.cs \
	MonoDevelop.CSharp.Resolver/TextEditorResolverProvider.cs \
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
	$(CSC) $(CSC_FLAGS) "-define:GMCS_SOURCE;NET_2_0;MS_COMPATIBLE;FULL_AST;BOOTSTRAP_BASIC" -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

#check: all $(TESTASSEMBLY)
#	MONO_PATH=$(top_builddir)/build/AddIns nunit-console $(TESTASSEMBLY)


$(TESTASSEMBLY) : $(build_test_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -debug "-define:GMCS_SOURCE;NET_2_0;MS_COMPATIBLE;FULL_AST;BOOTSTRAP_BASIC" -codepage:utf8 -out:$@ -target:library $(DLLS) $(TESTDLLS) $(REFS) /r:$(ASSEMBLY) \
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

