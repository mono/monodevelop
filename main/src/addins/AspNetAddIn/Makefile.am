ADDIN_BUILD = $(top_builddir)/build/AddIns/AspNetAddIn
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.AspNet.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.MsVisualStudio/MonoDevelop.MsVisualStudio.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:System \
	-r:System.Configuration \
	-r:System.Design \
	-r:System.Drawing \
	-r:System.Drawing.Design \
	-r:System.Web \
	-r:System.Xml

FILES =  \
	Deployment/WebDeployCommands.cs \
	Deployment/WebDeployLaunchDialog.cs \
	Deployment/WebDeployOptionsPanel.cs \
	Deployment/WebDeployOptionsPanelWidget.cs \
	Deployment/WebDeployResolver.cs \
	Deployment/WebDeployService.cs \
	Deployment/WebDeployTarget.cs \
	Deployment/WebDeployTargetCollection.cs \
	Deployment/WebDeployTargetEditor.cs \
	Deployment/WebTargetEditor.cs \
	gtk-gui/AspNetAddIn.AspNetConfigurationPanelWidget.cs \
	gtk-gui/AspNetAddIn.XspOptionsPanelWidget.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployLaunchDialog.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebDeployTargetEditor.cs \
	gtk-gui/MonoDevelop.AspNet.Deployment.WebTargetEditor.cs \
	Gui/AspNetConfigurationPanel.cs \
	Gui/AspNetConfigurationPanelWidget.cs \
	Gui/XspOptionsPanel.cs \
	Gui/XspOptionsPanelWidget.cs \
	Parser/Document.cs \
	Parser/DocumentReferenceManager.cs \
	Parser/Internal/AspParser.cs \
	Parser/Internal/AspTokenizer.cs \
	Parser/Internal/Directive.cs \
	Parser/Internal/Location.cs \
	Parser/Internal/ParseException.cs \
	Parser/Internal/StrUtils.cs \
	Parser/Internal/TagAttributes.cs \
	Parser/Internal/TagType.cs \
	Parser/LocatedParserException.cs \
	Parser/MemberListVisitor.cs \
	Parser/PageInfoVisitor.cs \
	Parser/Tree/CodeRenderNode.cs \
	Parser/Tree/DirectiveNode.cs \
	Parser/Tree/ILocation.cs \
	Parser/Tree/Node.cs \
	Parser/Tree/ParentNode.cs \
	Parser/Tree/RootNode.cs \
	Parser/Tree/ServerCommentNode.cs \
	Parser/Tree/ServerIncludeNode.cs \
	Parser/Tree/TagNode.cs \
	Parser/Tree/TextNode.cs \
	Parser/Tree/Visitor.cs \
	Parser/WebFormReferenceManager.cs \
	Project/AspNetAppProject.cs \
	Project/AspNetAppProjectBinding.cs \
	Project/AspNetAppProjectConfiguration.cs \
	Project/AspNetCodeBehindProvider.cs \
	Project/AspNetCommands.cs \
	Project/AspNetDisplayBinding.cs \
	Project/AspNetFileDescriptionTemplate.cs \
	Project/AspNetMSBuildProjectExtension.cs \
	Project/CodeBehind.cs \
	Project/ProjectFolderNodeBuilderExtension.cs \
	Project/VerifyCodeBehindBuildStep.cs \
	Project/WebTypeManager.cs \
	Project/XspParameters.cs

RES =  \
	AspNetAddIn.addin.xml \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
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


if ENABLE_ASPNET
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

check: all

if ENABLE_ASPNET
csharpbindinglibdir = $(MD_ADDIN_DIR)/AspNetAddIn
csharpbindinglib_DATA = $(ASSEMBLY)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

