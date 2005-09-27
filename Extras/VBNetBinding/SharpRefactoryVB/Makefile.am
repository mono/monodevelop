FILES = \
src/AssemblyInfo.cs \
src/CodeDom/CodeDOMVisitor.cs \
src/CSharpConverter/CSharpVisitor.cs \
src/Lexer/Lexer.cs \
src/Lexer/Reader/FileReader.cs \
src/Lexer/Reader/IReader.cs \
src/Lexer/Reader/StringReader.cs \
src/Lexer/Specials/Comment.cs \
src/Lexer/Specials/CommentType.cs \
src/Lexer/Specials/NewLine.cs \
src/Lexer/Specials/PreProcessingDirective.cs \
src/Lexer/Specials/SpecialTracker.cs \
src/Lexer/Specials/TagComment.cs \
src/Main.cs \
src/Parser/AST/AbstractASTVisitor.cs \
src/Parser/AST/AbstractNode.cs \
src/Parser/AST/CompilationUnit.cs \
src/Parser/AST/Enums.cs \
src/Parser/AST/Expressions/AddressOfExpression.cs \
src/Parser/AST/Expressions/ArrayCreateExpression.cs \
src/Parser/AST/Expressions/ArrayInitializerExpression.cs \
src/Parser/AST/Expressions/AssignmentExpression.cs \
src/Parser/AST/Expressions/BaseReferenceExpression.cs \
src/Parser/AST/Expressions/BinaryOperatorExpression.cs \
src/Parser/AST/Expressions/CastExpression.cs \
src/Parser/AST/Expressions/ClassReferenceExpression.cs \
src/Parser/AST/Expressions/Expression.cs \
src/Parser/AST/Expressions/FieldReferenceOrInvocationExpression.cs \
src/Parser/AST/Expressions/GetTypeExpression.cs \
src/Parser/AST/Expressions/IdentifierExpression.cs \
src/Parser/AST/Expressions/InvocationExpression.cs \
src/Parser/AST/Expressions/LoopControlVariableExpression.cs \
src/Parser/AST/Expressions/NamedArgumentExpression.cs \
src/Parser/AST/Expressions/ObjectCreateExpression.cs \
src/Parser/AST/Expressions/ParameterDeclarationExpression.cs \
src/Parser/AST/Expressions/ParenthesizedExpression.cs \
src/Parser/AST/Expressions/PrimitiveExpression.cs \
src/Parser/AST/Expressions/StatementExpression.cs \
src/Parser/AST/Expressions/ThisReferenceExpression.cs \
src/Parser/AST/Expressions/TypeOfExpression.cs \
src/Parser/AST/Expressions/TypeReferenceExpression.cs \
src/Parser/AST/Expressions/UnaryOperatorExpression.cs \
src/Parser/AST/GlobalScope/AttributeDeclaration.cs \
src/Parser/AST/GlobalScope/DelegateDeclaration.cs \
src/Parser/AST/GlobalScope/ImportsAliasDeclaration.cs \
src/Parser/AST/GlobalScope/ImportsDeclaration.cs \
src/Parser/AST/GlobalScope/ImportsStatement.cs \
src/Parser/AST/GlobalScope/NamespaceDeclaration.cs \
src/Parser/AST/GlobalScope/OptionCompareDeclaration.cs \
src/Parser/AST/GlobalScope/OptionExplicitDeclaration.cs \
src/Parser/AST/GlobalScope/OptionStrictDeclaration.cs \
src/Parser/AST/GlobalScope/TypeDeclaration.cs \
src/Parser/AST/IASTVisitor.cs \
src/Parser/AST/INode.cs \
src/Parser/AST/LookupTableVisitor.cs \
src/Parser/AST/Modifier.cs \
src/Parser/AST/Statements/AddHandlerStatement.cs \
src/Parser/AST/Statements/BlockStatement.cs \
src/Parser/AST/Statements/DoLoopStatement.cs \
src/Parser/AST/Statements/EndStatement.cs \
src/Parser/AST/Statements/EraseStatements.cs \
src/Parser/AST/Statements/ErrorStatement.cs \
src/Parser/AST/Statements/ExitStatement.cs \
src/Parser/AST/Statements/ForeachStatement.cs \
src/Parser/AST/Statements/ForStatement.cs \
src/Parser/AST/Statements/GoToStatement.cs \
src/Parser/AST/Statements/IfStatement.cs \
src/Parser/AST/Statements/LabelStatement.cs \
src/Parser/AST/Statements/LocalVariableDeclaration.cs \
src/Parser/AST/Statements/LockStatement.cs \
src/Parser/AST/Statements/OnErrorStatement.cs \
src/Parser/AST/Statements/RaiseEventStatement.cs \
src/Parser/AST/Statements/ReDimStatement.cs \
src/Parser/AST/Statements/RemoveHandlerStatement.cs \
src/Parser/AST/Statements/ResumeStatement.cs \
src/Parser/AST/Statements/ReturnStatement.cs \
src/Parser/AST/Statements/SelectStatement.cs \
src/Parser/AST/Statements/Statement.cs \
src/Parser/AST/Statements/StopStatement.cs \
src/Parser/AST/Statements/ThrowStatement.cs \
src/Parser/AST/Statements/TryCatchStatement.cs \
src/Parser/AST/Statements/WhileStatement.cs \
src/Parser/AST/Statements/WithStatement.cs \
src/Parser/AST/TypeLevel/ConstructorDeclaration.cs \
src/Parser/AST/TypeLevel/DeclareDeclaration.cs \
src/Parser/AST/TypeLevel/EventDeclaration.cs \
src/Parser/AST/TypeLevel/FieldDeclaration.cs \
src/Parser/AST/TypeLevel/HandlesClause.cs \
src/Parser/AST/TypeLevel/ImplementsClause.cs \
src/Parser/AST/TypeLevel/MethodDeclaration.cs \
src/Parser/AST/TypeLevel/PropertyDeclaration.cs \
src/Parser/AST/TypeLevel/PropertyGetRegion.cs \
src/Parser/AST/TypeLevel/PropertySetRegion.cs \
src/Parser/AST/TypeLevel/TypeReference.cs \
src/Parser/AST/TypeLevel/VariableDeclaration.cs \
src/Parser/generated/Errors.cs \
src/Parser/generated/keywordlist/Keywords.cs \
src/Parser/generated/keywordlist/Tokens.cs \
src/Parser/generated/Modifiers.cs \
src/Parser/generated/ParamModifiers.cs \
src/Parser/generated/Parser.cs

build_sources = $(addprefix $(srcdir)/, $(FILES))

DLL = $(top_builddir)/build/bin/ICSharpCode.SharpRefactory.VB.dll

REFS = /r:System.Drawing.dll

all: $(DLL)

$(DLL): $(FILES)
	$(CSC) $(CSC_FLAGS) /out:$(DLL) /target:library $(REFS) $(build_sources)

test-parser.exe : $(DLL) src/Main.cs
	$(CSC) /out:$@ /r:$(DLL) src/Main.cs

run : test-parser.exe
	MONO_PATH=`dirname $(DLL)` $(RUNTIME) test-parser.exe $(TEST_SOURCE)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(DLL)

CLEANFILES = $(DLL) $(DLL).mdb

EXTRA_DIST = $(FILES)

