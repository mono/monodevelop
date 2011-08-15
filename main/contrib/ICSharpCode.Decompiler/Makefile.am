	
ASSEMBLY = $(top_builddir)/build/bin/ICSharpCode.Decompiler.dll
  

REFS =  \
	-r:../../build/bin/ICSharpCode.NRefactory.dll \
	-r:../../build/bin/Mono.Cecil.dll \
	-r:System \
	-r:System.Core \
	-r:System.Xml \
	-r:System.Xml.Linq
	

FILES =  \
	Ast/Annotations.cs \
	Ast/AstBuilder.cs \
	Ast/AstMethodBodyBuilder.cs \
	Ast/CecilTypeResolveContext.cs \
	Ast/CommentStatement.cs \
	Ast/DecompilerContext.cs \
	Ast/NameVariables.cs \
	Ast/NRefactoryExtensions.cs \
	Ast/TextOutputFormatter.cs \
	Ast/Transforms/AddCheckedBlocks.cs \
	Ast/Transforms/CombineQueryExpressions.cs \
	Ast/Transforms/ContextTrackingVisitor.cs \
	Ast/Transforms/ConvertConstructorCallIntoInitializer.cs \
	Ast/Transforms/DecimalConstantTransform.cs \
	Ast/Transforms/DeclareVariables.cs \
	Ast/Transforms/DelegateConstruction.cs \
	Ast/Transforms/IntroduceExtensionMethods.cs \
	Ast/Transforms/IntroduceQueryExpressions.cs \
	Ast/Transforms/IntroduceUnsafeModifier.cs \
	Ast/Transforms/IntroduceUsingDeclarations.cs \
	Ast/Transforms/PatternStatementTransform.cs \
	Ast/Transforms/PushNegation.cs \
	Ast/Transforms/ReplaceMethodCallsWithOperators.cs \
	Ast/Transforms/TransformationPipeline.cs \
	Ast/TypesHierarchyHelpers.cs \
	CecilExtensions.cs \
	CodeMappings.cs \
	DecompilerException.cs \
	DecompilerSettings.cs \
	Disassembler/DisassemblerHelpers.cs \
	Disassembler/ILStructure.cs \
	Disassembler/MethodBodyDisassembler.cs \
	Disassembler/ReflectionDisassembler.cs \
	FlowAnalysis/ControlFlowEdge.cs \
	FlowAnalysis/ControlFlowGraph.cs \
	FlowAnalysis/ControlFlowGraphBuilder.cs \
	FlowAnalysis/ControlFlowNode.cs \
	FlowAnalysis/ControlStructureDetector.cs \
	FlowAnalysis/OpCodeInfo.cs \
	FlowAnalysis/SimplifyByRefCalls.cs \
	FlowAnalysis/SsaBlock.cs \
	FlowAnalysis/SsaForm.cs \
	FlowAnalysis/SsaFormBuilder.cs \
	FlowAnalysis/SsaInstruction.cs \
	FlowAnalysis/SsaOptimization.cs \
	FlowAnalysis/SsaVariable.cs \
	FlowAnalysis/TransformToSsa.cs \
	ILAst/DefaultDictionary.cs \
	ILAst/GotoRemoval.cs \
	ILAst/ILAstBuilder.cs \
	ILAst/ILAstOptimizer.cs \
	ILAst/ILAstTypes.cs \
	ILAst/ILCodes.cs \
	ILAst/ILInlining.cs \
	ILAst/InitializerPeepholeTransforms.cs \
	ILAst/LoopsAndConditions.cs \
	ILAst/PatternMatching.cs \
	ILAst/PeepholeTransform.cs \
	ILAst/SimpleControlFlow.cs \
	ILAst/TypeAnalysis.cs \
	ILAst/YieldReturnDecompiler.cs \
	ITextOutput.cs \
	PlainTextOutput.cs \
	Properties/AssemblyInfo.cs \
	ReferenceResolvingException.cs \
	Tests/CallOverloadedMethod.cs \
	TextOutputWriter.cs 
all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) -define:NET35 -debug -out:$@ -target:library $(REFS) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(SIGNING_KEY)

include $(top_srcdir)/Makefile.include
