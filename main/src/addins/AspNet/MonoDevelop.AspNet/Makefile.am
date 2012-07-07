ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.AspNet
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.AspNet.dll

DEPS =  \
	$(top_builddir)/build/AddIns/DisplayBindings/SourceEditor/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.Linux.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.XmlEditor/MonoDevelop.XmlEditor.dll \
	$(top_builddir)/build/bin/ICSharpCode.NRefactory.CSharp.dll \
	$(top_builddir)/build/bin/ICSharpCode.NRefactory.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Configuration \
	-r:System.Core \
	-r:System.Design \
	-r:System.Drawing \
	-r:System.Drawing.Design \
	-r:System.Web \
	-r:System.Xml \
	-r:System.Xml.Linq

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployLaunchDialog.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployTargetEditor.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebTargetEditor.cs \
	gtk-gui/MonoDevelop.AspNet.Gui.AspNetConfigurationPanelWidget.cs \
	gtk-gui/MonoDevelop.AspNet.Gui.XspOptionsPanelWidget.cs \
	MonoDevelop.AspNet.Completion/SimpleLists.cs \
	MonoDevelop.AspNet.Deployment/AspNetDeployServiceExtension.cs \
	MonoDevelop.AspNet.Deployment/WebDeployCommands.cs \
	MonoDevelop.AspNet.Deployment/WebDeployLaunchDialog.cs \
	MonoDevelop.AspNet.Deployment/WebDeployOptionsPanel.cs \
	MonoDevelop.AspNet.Deployment/WebDeployOptionsPanelWidget.cs \
	MonoDevelop.AspNet.Deployment/WebDeployResolver.cs \
	MonoDevelop.AspNet.Deployment/WebDeployService.cs \
	MonoDevelop.AspNet.Deployment/WebDeployTarget.cs \
	MonoDevelop.AspNet.Deployment/WebDeployTargetCollection.cs \
	MonoDevelop.AspNet.Deployment/WebDeployTargetEditor.cs \
	MonoDevelop.AspNet.Deployment/WebTargetEditor.cs \
	MonoDevelop.AspNet.Gui/AspNetConfigurationPanel.cs \
	MonoDevelop.AspNet.Gui/AspNetConfigurationPanelWidget.cs \
	MonoDevelop.AspNet.Gui/AspNetEditorExtension.cs \
	MonoDevelop.AspNet.Gui/AspProjectDom.cs \
	MonoDevelop.AspNet.Gui/BaseHtmlEditorExtension.cs \
	MonoDevelop.AspNet.Gui/BrowserLauncher.cs \
	MonoDevelop.AspNet.Gui/HtmlEditorExtension.cs \
	MonoDevelop.AspNet.Gui/ILanguageCompletionBuilder.cs \
	MonoDevelop.AspNet.Gui/TypeResolveContextDecorator.cs \
	MonoDevelop.AspNet.Gui/XspOptionsPanel.cs \
	MonoDevelop.AspNet.Gui/XspOptionsPanelWidget.cs \
	MonoDevelop.AspNet.MD1Serialization/MD1AspNetCustomDataItem.cs \
	MonoDevelop.AspNet.Parser.Dom/DirectiveNode.cs \
	MonoDevelop.AspNet.Parser.Dom/ExpressionNode.cs \
	MonoDevelop.AspNet.Parser.Dom/ILocation.cs \
	MonoDevelop.AspNet.Parser.Dom/Node.cs \
	MonoDevelop.AspNet.Parser.Dom/ParentNode.cs \
	MonoDevelop.AspNet.Parser.Dom/RootNode.cs \
	MonoDevelop.AspNet.Parser.Dom/ServerCommentNode.cs \
	MonoDevelop.AspNet.Parser.Dom/ServerIncludeNode.cs \
	MonoDevelop.AspNet.Parser.Dom/TagNode.cs \
	MonoDevelop.AspNet.Parser.Dom/TextNode.cs \
	MonoDevelop.AspNet.Parser.Dom/Visitor.cs \
	MonoDevelop.AspNet.Parser.Internal/AspParser.cs \
	MonoDevelop.AspNet.Parser.Internal/AspTokenizer.cs \
	MonoDevelop.AspNet.Parser.Internal/Directive.cs \
	MonoDevelop.AspNet.Parser.Internal/Location.cs \
	MonoDevelop.AspNet.Parser.Internal/ParseException.cs \
	MonoDevelop.AspNet.Parser.Internal/StrUtils.cs \
	MonoDevelop.AspNet.Parser.Internal/TagAttributes.cs \
	MonoDevelop.AspNet.Parser.Internal/TagType.cs \
	MonoDevelop.AspNet.Parser/AspNetParsedDocument.cs \
	MonoDevelop.AspNet.Parser/AspNetParser.cs \
	MonoDevelop.AspNet.Parser/CompilationUnitVisitor.cs \
	MonoDevelop.AspNet.Parser/DebugStringVisitor.cs \
	MonoDevelop.AspNet.Parser/Directive.cs \
	MonoDevelop.AspNet.Parser/DocumentReferenceManager.cs \
	MonoDevelop.AspNet.Parser/LocatedParserException.cs \
	MonoDevelop.AspNet.Parser/MemberListBuilder.cs \
	MonoDevelop.AspNet.Parser/PageInfo.cs \
	MonoDevelop.AspNet.Parser/Regexes.cs \
	MonoDevelop.AspNet.Parser/SuggestedHandlerCompletionData.cs \
	MonoDevelop.AspNet.Parser/WebFormReferenceManager.cs \
	MonoDevelop.AspNet.StateEngine/AspNetDirectiveState.cs \
	MonoDevelop.AspNet.StateEngine/AspNetDom.cs \
	MonoDevelop.AspNet.StateEngine/AspNetExpressionState.cs \
	MonoDevelop.AspNet.StateEngine/AspNetFreeState.cs \
	MonoDevelop.AspNet.StateEngine/AspNetServerCommentState.cs \
	MonoDevelop.AspNet.StateEngine/AspNetSpeculativeExpressionState.cs \
	MonoDevelop.AspNet.StateEngine/HtmlClosingTagState.cs \
	MonoDevelop.AspNet.StateEngine/HtmlScriptBodyState.cs \
	MonoDevelop.AspNet.StateEngine/HtmlTagState.cs \
	MonoDevelop.AspNet/AspNetAppProject.cs \
	MonoDevelop.AspNet/AspNetAppProjectBinding.cs \
	MonoDevelop.AspNet/AspNetAppProjectConfiguration.cs \
	MonoDevelop.AspNet/AspNetCommands.cs \
	MonoDevelop.AspNet/AspNetExecutionCommand.cs \
	MonoDevelop.AspNet/AspNetExecutionHandler.cs \
	MonoDevelop.AspNet/AspNetFileDescriptionTemplate.cs \
	MonoDevelop.AspNet/AspNetMSBuildImportProvider.cs \
	MonoDevelop.AspNet/AspNetParserService.cs \
	MonoDevelop.AspNet/AspNetSyntaxMode.cs \
	MonoDevelop.AspNet/AspNetToolboxLoader.cs \
	MonoDevelop.AspNet/AspNetToolboxNode.cs \
	MonoDevelop.AspNet/AspNetToolboxProvider.cs \
	MonoDevelop.AspNet/CodeBehind.cs \
	MonoDevelop.AspNet/IAspNetLanguageBinding.cs \
	MonoDevelop.AspNet/LineInterceptingTextWriter.cs \
	MonoDevelop.AspNet/MasterContentFileDescriptionTemplate.cs \
	MonoDevelop.AspNet/ProjectFolderNodeBuilderExtension.cs \
	MonoDevelop.AspNet/RegistrationCache.cs \
	MonoDevelop.AspNet/VerifyCodeBehindBuildStep.cs \
	MonoDevelop.AspNet/WebTypeManager.cs \
	MonoDevelop.AspNet/XspParameters.cs \
	MonoDevelop.Html/DocTypeCompletionData.cs \
	MonoDevelop.Html/DocTypeExtensionNode.cs \
	MonoDevelop.Html/ElementTypes.cs \
	MonoDevelop.Html/HtmlCompletionDataProvider.cs \
	MonoDevelop.Html/HtmlParser.cs \
	MonoDevelop.Html/HtmlSchema.cs \
	MonoDevelop.Html/HtmlSchemaService.cs \
	MonoDevelop.Html/PathCompletion.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.AspNet.addin.xml \
	MonoDevelop.AspNet.MD1Serialization/md1format.xml \
	Templates/GlobalAsax-CodeBehind.xft.xml \
	Templates/GlobalAsax-Empty.xft.xml \
	Templates/MasterPage-CodeBehind.xft.xml \
	Templates/MasterPage-CodeBehindNonPartial.xft.xml \
	Templates/MasterPage-Empty.xft.xml \
	Templates/NestedMasterPage.xft.xml \
	Templates/NestedMasterPage-CodeBehind.xft.xml \
	Templates/WebApplication.xpt.xml \
	Templates/WebApplication-Empty.xpt.xml \
	Templates/WebConfig-Application.xft.xml \
	Templates/WebConfig-SubDir.xft.xml \
	Templates/WebContentForm.xft.xml \
	Templates/WebContentForm-CodeBehind.xft.xml \
	Templates/WebControl-CodeBehind.xft.xml \
	Templates/WebControl-CodeBehindNonPartial.xft.xml \
	Templates/WebControl-Empty.xft.xml \
	Templates/WebForm-CodeBehind.xft.xml \
	Templates/WebForm-CodeBehindNonPartial.xft.xml \
	Templates/WebForm-Empty.xft.xml \
	Templates/WebHandler-CodeBehind.xft.xml \
	Templates/WebHandler-Empty.xft.xml \
	Templates/WebService-CodeBehind.xft.xml \
	Templates/WebService-Empty.xft.xml

DATA_FILES =  \
	Schemas/readme.txt \
	Schemas/W3C-License.html \
	Schemas/xhtml1-frameset.xsd \
	Schemas/xhtml1-strict.xsd \
	Schemas/xhtml1-transitional.xsd 

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/Schemas/, $(notdir $(DATA_FILES)))

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)/Schemas
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $@

check: all

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.AspNet
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

schemadir = $(MD_ADDIN_DIR)/MonoDevelop.AspNet/Schemas
schema_DATA = $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include

