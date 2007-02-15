	
ASSEMBLY = $(top_builddir)/build/bin/NRefactory.dll
  

REFS =  \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	Project/Configuration/AssemblyInfo.cs \
	Project/Src/Lexer/AbstractLexer.cs \
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
	Project/Src/Output/AbstractOutputFormatter.cs \
	Project/Src/Output/AbstractPrettyPrintOptions.cs \
	Project/Src/Output/CodeDOM/CodeDOMOutputVisitor.cs \
	Project/Src/Output/CodeDOM/CodeDOMVerboseOutputGenerator.cs \
	Project/Src/Output/CSharp/CSharpOutputVisitor.cs \
	Project/Src/Output/CSharp/OutputFormatter.cs \
	Project/Src/Output/CSharp/PrettyPrintOptions.cs \
	Project/Src/Output/EnvironmentInformationProvider.cs \
	Project/Src/Output/IOutputASTVisitor.cs \
	Project/Src/Output/NodeInformVisitor.cs \
	Project/Src/Output/SpecialNodesInserter.cs \
	Project/Src/Output/VBNet/VBNetOutputFormatter.cs \
	Project/Src/Output/VBNet/VBNetOutputVisitor.cs \
	Project/Src/Output/VBNet/VBNetPrettyPrintOptions.cs \
	Project/Src/Parser/AbstractParser.cs \
	Project/Src/Parser/AST/AbstractNode.cs \
	Project/Src/Parser/AST/Enums.cs \
	Project/Src/Parser/AST/General/BlockStatement.cs \
	Project/Src/Parser/AST/General/CompilationUnit.cs \
	Project/Src/Parser/AST/General/Expression.cs \
	Project/Src/Parser/AST/General/LocalVariableDeclaration.cs \
	Project/Src/Parser/AST/General/PrimitiveExpression.cs \
	Project/Src/Parser/AST/General/Statement.cs \
	Project/Src/Parser/AST/Generated.cs \
	Project/Src/Parser/AST/INode.cs \
	Project/Src/Parser/AST/INullable.cs \
	Project/Src/Parser/AST/TypeReference.cs \
	Project/Src/Parser/CSharp/Parser.cs \
	Project/Src/Parser/Errors.cs \
	Project/Src/Parser/IParser.cs \
	Project/Src/Parser/Modifiers.cs \
	Project/Src/Parser/ParserFactory.cs \
	Project/Src/Parser/VBNet/ParamModifier.cs \
	Project/Src/Parser/VBNet/Parser.cs \
	Project/Src/Parser/Visitors/AbstractAstTransformer.cs \
	Project/Src/Parser/Visitors/AbstractASTVisitor.cs \
	Project/Src/Parser/Visitors/CSharpConstructsVisitor.cs \
	Project/Src/Parser/Visitors/CSharpToVBNetConvertVisitor.cs \
	Project/Src/Parser/Visitors/IASTVisitor.cs \
	Project/Src/Parser/Visitors/LookupTableVisitor.cs \
	Project/Src/Parser/Visitors/PrefixFieldsVisitor.cs \
	Project/Src/Parser/Visitors/ToCSharpConvertVisitor.cs \
	Project/Src/Parser/Visitors/ToVBNetConvertVisitor.cs \
	Project/Src/Parser/Visitors/VBNetConstructsConvertVisitor.cs \
	Project/Src/Parser/Visitors/VBNetToCSharpConvertVisitor.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include
