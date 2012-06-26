
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Refactoring
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Refactoring.dll

DEPS =  \
	$(top_builddir)/build/AddIns/DisplayBindings/SourceEditor/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/bin/ICSharpCode.NRefactory.CSharp.dll \
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
	gtk-gui/MonoDevelop.CodeActions.ContextActionPanelWidget.cs \
	gtk-gui/MonoDevelop.CodeIssues.CodeIssueOptionsDialog.cs \
	gtk-gui/MonoDevelop.CodeIssues.CodeIssuePanelWidget.cs \
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
	MonoDevelop.CodeActions/AnalysisCodeAction.cs \
	MonoDevelop.CodeActions/CodeAction.cs \
	MonoDevelop.CodeActions/CodeActionAddinNode.cs \
	MonoDevelop.CodeActions/CodeActionEditorExtension.cs \
	MonoDevelop.CodeActions/CodeActionPanelWidget.cs \
	MonoDevelop.CodeActions/CodeActionProvider.cs \
	MonoDevelop.CodeActions/CodeActionWidget.cs \
	MonoDevelop.CodeActions/ICodeActionProviderSource.cs \
	MonoDevelop.CodeIssues/CodeAnalysisRunner.cs \
	MonoDevelop.CodeIssues/CodeIssue.cs \
	MonoDevelop.CodeIssues/CodeIssueAddinNode.cs \
	MonoDevelop.CodeIssues/CodeIssueOptionsDialog.cs \
	MonoDevelop.CodeIssues/CodeIssuePanelWidget.cs \
	MonoDevelop.CodeIssues/CodeIssueProvider.cs \
	MonoDevelop.CodeIssues/DefaultCodeIssueCategories.cs \
	MonoDevelop.CodeIssues/ICodeIssueProviderSource.cs \
	MonoDevelop.Refactoring.ImplementInterface/ImplementAbstractMembers.cs \
	MonoDevelop.Refactoring.ImplementInterface/ImplementExplicit.cs \
	MonoDevelop.Refactoring.ImplementInterface/ImplementImplicit.cs \
	MonoDevelop.Refactoring.Rename/RenameHandler.cs \
	MonoDevelop.Refactoring.Rename/RenameItemDialog.cs \
	MonoDevelop.Refactoring.Rename/RenameRefactoring.cs \
	MonoDevelop.Refactoring.Rename/RenameTextEditorExtension.cs \
	MonoDevelop.Refactoring/AbstractRefactoringCommandHandler.cs \
	MonoDevelop.Refactoring/Change.cs \
	MonoDevelop.Refactoring/FindDerivedClassesHandler.cs \
	MonoDevelop.Refactoring/FindReferencesHandler.cs \
	MonoDevelop.Refactoring/GotoDeclarationHandler.cs \
	MonoDevelop.Refactoring/ImportSymbolHandler.cs \
	MonoDevelop.Refactoring/RefactoringOperation.cs \
	MonoDevelop.Refactoring/RefactoringOptions.cs \
	MonoDevelop.Refactoring/RefactoringPreviewDialog.cs \
	MonoDevelop.Refactoring/RefactoringService.cs \
	MonoDevelop.Refactoring/RefactoryCommands.cs \
	MonoDevelop.Refactoring/ResolveCommandHandler.cs

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
