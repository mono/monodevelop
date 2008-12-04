	
ASSEMBLY = $(top_builddir)/build/bin/NRefactory.dll
  

REFS =  \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	Project/Configuration/AssemblyInfo.cs \
	Project/Src/Ast/AbstractNode.cs \
	Project/Src/Ast/Enums.cs \
	Project/Src/Ast/General/BlockStatement.cs \
	Project/Src/Ast/General/CompilationUnit.cs \
	Project/Src/Ast/General/Expression.cs \
	Project/Src/Ast/General/LocalVariableDeclaration.cs \
	Project/Src/Ast/General/PrimitiveExpression.cs \
	Project/Src/Ast/General/Statement.cs \
	Project/Src/Ast/Generated.cs \
	Project/Src/Ast/INode.cs \
	Project/Src/Ast/INullable.cs \
	Project/Src/Ast/TypeReference.cs \
	Project/Src/AstBuilder/ExpressionBuilder.cs \
	Project/Src/AstBuilder/StatementBuilder.cs \
	Project/Src/EnvironmentInformationProvider.cs \
	Project/Src/IAstVisitor.cs \
	Project/Src/Lexer/AbstractLexer.cs \
	Project/Src/Lexer/CSharp/ConditionalCompilation.cs \
	Project/Src/Lexer/CSharp/Keywords.cs \
	Project/Src/Lexer/CSharp/Lexer.cs \
	Project/Src/Lexer/CSharp/Tokens.cs \
	Project/Src/Lexer/ILexer.cs \
	Project/Src/Lexer/LookupTable.cs \
	Project/Src/Lexer/Special/BlankLine.cs \
	Project/Src/Lexer/Special/Comment.cs \
	Project/Src/Lexer/Special/CommentType.cs \
	Project/Src/Lexer/Special/ISpecial.cs \
	Project/Src/Lexer/Special/PreProcessingDirective.cs \
	Project/Src/Lexer/Special/SpecialTracker.cs \
	Project/Src/Lexer/Special/TagComment.cs \
	Project/Src/Lexer/Token.cs \
	Project/Src/Lexer/VBNet/Keywords.cs \
	Project/Src/Lexer/VBNet/Lexer.cs \
	Project/Src/Lexer/VBNet/Tokens.cs \
	Project/Src/Location.cs \
	Project/Src/OperatorPrecedence.cs \
	Project/Src/Parser/AbstractParser.cs \
	Project/Src/Parser/CSharp/CSharpParser.cs \
	Project/Src/Parser/CSharp/Parser.cs \
	Project/Src/Parser/Errors.cs \
	Project/Src/Parser/IParser.cs \
	Project/Src/Parser/ModifierList.cs \
	Project/Src/Parser/VBNet/ParamModifierList.cs \
	Project/Src/Parser/VBNet/Parser.cs \
	Project/Src/Parser/VBNet/VBNetParser.cs \
	Project/Src/ParserFactory.cs \
	Project/Src/PrettyPrinter/AbstractOutputFormatter.cs \
	Project/Src/PrettyPrinter/AbstractPrettyPrintOptions.cs \
	Project/Src/PrettyPrinter/CSharp/CSharpOutputVisitor.cs \
	Project/Src/PrettyPrinter/CSharp/OutputFormatter.cs \
	Project/Src/PrettyPrinter/CSharp/PrettyPrintOptions.cs \
	Project/Src/PrettyPrinter/IOutputAstVisitor.cs \
	Project/Src/PrettyPrinter/SpecialNodesInserter.cs \
	Project/Src/PrettyPrinter/VBNet/VBNetOutputFormatter.cs \
	Project/Src/PrettyPrinter/VBNet/VBNetOutputVisitor.cs \
	Project/Src/PrettyPrinter/VBNet/VBNetPrettyPrintOptions.cs \
	Project/Src/SnippetParser.cs \
	Project/Src/Visitors/AbstractAstTransformer.cs \
	Project/Src/Visitors/AbstractASTVisitor.cs \
	Project/Src/Visitors/CodeDOMOutputVisitor.cs \
	Project/Src/Visitors/CodeDOMVerboseOutputGenerator.cs \
	Project/Src/Visitors/ConvertVisitorBase.cs \
	Project/Src/Visitors/CSharpConstructsConvertVisitor.cs \
	Project/Src/Visitors/LookupTableVisitor.cs \
	Project/Src/Visitors/NodeTrackingAstVisitor.cs \
	Project/Src/Visitors/PrefixFieldsVisitor.cs \
	Project/Src/Visitors/RenameIdentifierVisitor.cs \
	Project/Src/Visitors/ToCSharpConvertVisitor.cs \
	Project/Src/Visitors/ToVBNetConvertVisitor.cs \
	Project/Src/Visitors/ToVBNetRenameConflictingVariables.cs \
	Project/Src/Visitors/VBNetConstructsConvertVisitor.cs

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include
