ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.AspNet
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.AspNet.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.XmlEditor/MonoDevelop.XmlEditor.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:System \
	-r:System.Configuration \
	-r:System.Design \
	-r:System.Drawing \
	-r:System.Drawing.Design \
	-r:System.Web \
	-r:System.Xml

FILES =  \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployLaunchDialog.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployTargetEditor.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebTargetEditor.cs \
	gtk-gui/MonoDevelop.AspNet.Gui.AspNetConfigurationPanelWidget.cs \
	gtk-gui/MonoDevelop.AspNet.Gui.XspOptionsPanelWidget.cs \
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
	MonoDevelop.AspNet.Gui/BrowserLauncher.cs \
	MonoDevelop.AspNet.Gui/XspOptionsPanel.cs \
	MonoDevelop.AspNet.Gui/XspOptionsPanelWidget.cs \
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
	MonoDevelop.AspNet.Parser/AspNetCompilationUnit.cs \
	MonoDevelop.AspNet.Parser/AspNetExpressionFinder.cs \
	MonoDevelop.AspNet.Parser/AspNetParser.cs \
	MonoDevelop.AspNet.Parser/CompilationUnitVisitor.cs \
	MonoDevelop.AspNet.Parser/DebugStringVisitor.cs \
	MonoDevelop.AspNet.Parser/Document.cs \
	MonoDevelop.AspNet.Parser/DocumentReferenceManager.cs \
	MonoDevelop.AspNet.Parser/LocatedParserException.cs \
	MonoDevelop.AspNet.Parser/MemberListVisitor.cs \
	MonoDevelop.AspNet.Parser/PageDirectiveVisitor.cs \
	MonoDevelop.AspNet.Parser/PageInfo.cs \
	MonoDevelop.AspNet.Parser/PageInfoVisitor.cs \
	MonoDevelop.AspNet.Parser/Regexes.cs \
	MonoDevelop.AspNet.Parser/SuggestedHandlerCompletionData.cs \
	MonoDevelop.AspNet.Parser/WebFormReferenceManager.cs \
	MonoDevelop.AspNet.StateEngine/AspNetFreeState.cs \
	MonoDevelop.AspNet/AspNetAppProject.cs \
	MonoDevelop.AspNet/AspNetAppProjectBinding.cs \
	MonoDevelop.AspNet/AspNetAppProjectConfiguration.cs \
	MonoDevelop.AspNet/AspNetCommands.cs \
	MonoDevelop.AspNet/AspNetFileDescriptionTemplate.cs \
	MonoDevelop.AspNet/AspNetLanguageBinding.cs \
	MonoDevelop.AspNet/AspNetParserService.cs \
	MonoDevelop.AspNet/AspNetToolboxLoader.cs \
	MonoDevelop.AspNet/AspNetToolboxNode.cs \
	MonoDevelop.AspNet/AspNetToolboxProvider.cs \
	MonoDevelop.AspNet/CodeBehind.cs \
	MonoDevelop.AspNet/IAspNetLanguageBinding.cs \
	MonoDevelop.AspNet/ProjectFolderNodeBuilderExtension.cs \
	MonoDevelop.AspNet/VerifyCodeBehindBuildStep.cs \
	MonoDevelop.AspNet/WebTypeManager.cs \
	MonoDevelop.AspNet/XspParameters.cs \
	MonoDevelop.Html/DocTypeCompletionData.cs \
	MonoDevelop.Html/DocTypeCompletionDataProvider.cs \
	MonoDevelop.Html/DocTypeExtensionNode.cs \
	MonoDevelop.Html/HtmlCompletionDataProvider.cs \
	MonoDevelop.Html/HtmlSchema.cs \
	MonoDevelop.Html/HtmlSchemaService.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.AspNet.addin.xml \
	Templates/GlobalAsax-CodeBehind.xft.xml \
	Templates/GlobalAsax-Empty.xft.xml \
	Templates/MasterPage-CodeBehind.xft.xml \
	Templates/MasterPage-CodeBehindNonPartial.xft.xml \
	Templates/MasterPage-Empty.xft.xml \
	Templates/WebApplication.xpt.xml \
	Templates/WebApplication-Empty.xpt.xml \
	Templates/WebConfig-Application.xft.xml \
	Templates/WebConfig-SubDir.xft.xml \
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

all: $(ASSEMBLY) $(DATA_FILE_BUILD)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

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

