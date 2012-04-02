
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Refactoring
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Refactoring.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/bin/ICSharpCode.NRefactory.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.CodeGeneration.GenerateCodeWindow.cs \
	gtk-gui/MonoDevelop.ContextAction.ContextActionPanelWidget.cs \
	gtk-gui/MonoDevelop.Inspection.InspectionPanelWidget.cs \
	gtk-gui/MonoDevelop.Refactoring.EncapsulateFieldDialog.cs \
	gtk-gui/MonoDevelop.Refactoring.IntroduceConstant.IntroduceConstantDialog.cs \
	gtk-gui/MonoDevelop.Refactoring.OverridesImplementsDialog.cs \
	gtk-gui/MonoDevelop.Refactoring.RefactoringPreviewDialog.cs \
	gtk-gui/MonoDevelop.Refactoring.Rename.RenameItemDialog.cs \
	MonoDevelop.AnalysisCore/AnalysisCommands.cs \
	MonoDevelop.AnalysisCore/AnalysisExtensions.cs \
	MonoDevelop.AnalysisCore/AnalysisOptions.cs \
	MonoDevelop.AnalysisCore/AnalysisService.cs \
	MonoDevelop.AnalysisCore/Extensions/AnalysisRuleAddinNode.cs \
	MonoDevelop.AnalysisCore/Extensions/AnalysisTypeExtensionNode.cs \
	MonoDevelop.AnalysisCore/Extensions/FixHandlerExtensionNode.cs \
	MonoDevelop.AnalysisCore/FixableResult.cs \
	MonoDevelop.AnalysisCore/Fixes/GenericFix.cs \
	MonoDevelop.AnalysisCore/Fixes/RenameMemberFix.cs \
	MonoDevelop.AnalysisCore/Gui/AnalysisOptionsPanel.cs \
	MonoDevelop.AnalysisCore/Gui/ResultMarker.cs \
	MonoDevelop.AnalysisCore/Gui/ResultsEditorExtension.cs \
	MonoDevelop.AnalysisCore/Gui/ResultTooltipProvider.cs \
	MonoDevelop.AnalysisCore/Result.cs \
	MonoDevelop.AnalysisCore/Rules/Adaptors.cs \
	MonoDevelop.AnalysisCore/RuleTree.cs \
	MonoDevelop.AnalysisCore/RuleTreeType.cs \
	MonoDevelop.CodeGeneration/AbstractGenerateAction.cs \
	MonoDevelop.CodeGeneration/CodeGenerationCommands.cs \
	MonoDevelop.CodeGeneration/CodeGenerationOptions.cs \
	MonoDevelop.CodeGeneration/CreateConstructorGenerator.cs \
	MonoDevelop.CodeGeneration/EqualityMembersGenerator.cs \
	MonoDevelop.CodeGeneration/GenerateCodeWindow.cs \
	MonoDevelop.CodeGeneration/ICodeGenerator.cs \
	MonoDevelop.CodeGeneration/NullCheckGenerator.cs \
	MonoDevelop.CodeGeneration/OverrideMembersGenerator.cs \
	MonoDevelop.CodeGeneration/PropertyGenerator.cs \
	MonoDevelop.CodeGeneration/RaiseEventMethodGenerator.cs \
	MonoDevelop.CodeGeneration/ReadonlyPropertyGenerator.cs \
	MonoDevelop.CodeGeneration/ToStringGenerator.cs \
	MonoDevelop.CodeGeneration/WriteLineGenerator.cs \
	MonoDevelop.ContextAction/AnalysisContextAction.cs \
	MonoDevelop.ContextAction/ContextAction.cs \
	MonoDevelop.ContextAction/ContextActionAddinNode.cs \
	MonoDevelop.ContextAction/ContextActionEditorExtension.cs \
	MonoDevelop.ContextAction/ContextActionPanelWidget.cs \
	MonoDevelop.ContextAction/ContextActionWidget.cs \
	MonoDevelop.Inspection/InspectionPanelWidget.cs \
	MonoDevelop.Inspection/InspectorAddinNode.cs \
	MonoDevelop.Refactoring.CreateClass/CreateClassCodeGenerator.cs \
	MonoDevelop.Refactoring.CreateClass/CreateStructCodeGenerator.cs \
	MonoDevelop.Refactoring.ImplementInterface/ImplementAbstractMembers.cs \
	MonoDevelop.Refactoring.ImplementInterface/ImplementExplicit.cs \
	MonoDevelop.Refactoring.ImplementInterface/ImplementImplicit.cs \
	MonoDevelop.Refactoring.IntegrateTemporaryVariable/IntegrateTemporaryVariableHandler.cs \
	MonoDevelop.Refactoring.IntegrateTemporaryVariable/IntegrateTemporaryVariableRefactoring.cs \
	MonoDevelop.Refactoring.IntroduceConstant/IntroduceConstantDialog.cs \
	MonoDevelop.Refactoring.IntroduceConstant/IntroduceConstantHandler.cs \
	MonoDevelop.Refactoring.IntroduceConstant/IntroduceConstantRefactoring.cs \
	MonoDevelop.Refactoring.RefactorImports/FindTypeReferencesVisitor.cs \
	MonoDevelop.Refactoring.RefactorImports/RefactorImportsHandler.cs \
	MonoDevelop.Refactoring.RefactorImports/RemoveUnusedImportsRefactoring.cs \
	MonoDevelop.Refactoring.RefactorImports/SortImportsRefactoring.cs \
	MonoDevelop.Refactoring.Rename/RenameHandler.cs \
	MonoDevelop.Refactoring.Rename/RenameItemDialog.cs \
	MonoDevelop.Refactoring.Rename/RenameRefactoring.cs \
	MonoDevelop.Refactoring.Rename/RenameTextEditorExtension.cs \
	MonoDevelop.Refactoring/AbstractRefactoringCommandHandler.cs \
	MonoDevelop.Refactoring/Change.cs \
	MonoDevelop.Refactoring/EncapsulateFieldDialog.cs \
	MonoDevelop.Refactoring/FindDerivedClassesHandler.cs \
	MonoDevelop.Refactoring/FindReferencesHandler.cs \
	MonoDevelop.Refactoring/GotoDeclarationHandler.cs \
	MonoDevelop.Refactoring/HelperMethods.cs \
	MonoDevelop.Refactoring/ImportSymbolHandler.cs \
	MonoDevelop.Refactoring/INRefactoryASTProvider.cs \
	MonoDevelop.Refactoring/OverridesImplementsDialog.cs \
	MonoDevelop.Refactoring/QuickFixHandler.cs \
	MonoDevelop.Refactoring/RefactoringOperation.cs \
	MonoDevelop.Refactoring/RefactoringOptions.cs \
	MonoDevelop.Refactoring/RefactoringPreviewDialog.cs \
	MonoDevelop.Refactoring/RefactoringService.cs \
	MonoDevelop.Refactoring/RefactoryCommands.cs \
	MonoDevelop.Refactoring/ReferencesFinder.cs 

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Refactoring.addin.xml 

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Refactoring
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
