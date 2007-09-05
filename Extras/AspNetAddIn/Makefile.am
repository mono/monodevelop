ADDIN_BUILD = $(top_builddir)/build/AddIns/AspNetAddIn
ASSEMBLY = $(ADDIN_BUILD)/AspNetAddIn.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Deployment.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:System \
	-r:System.Design \
	-r:System.Drawing \
	-r:System.Drawing.Design \
	-r:System.Web \
	-r:System.Xml

FILES =  \
	gtk-gui/AspNetAddIn.AspNetConfigurationPanelWidget.cs \
	gtk-gui/generated.cs \
	Gui/AspNetConfigurationPanel.cs \
	Gui/AspNetConfigurationPanelWidget.cs \
	Gui/XspOptionsPanel.cs \
	Parser/Document.cs \
	Parser/Internal/AspParser.cs \
	Parser/Internal/AspTokenizer.cs \
	Parser/Internal/Directive.cs \
	Parser/Internal/Location.cs \
	Parser/Internal/ParseException.cs \
	Parser/Internal/StrUtils.cs \
	Parser/Internal/TagAttributes.cs \
	Parser/Internal/TagType.cs \
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
	Project/AspNetDisplayBinding.cs \
	Project/AspNetFileDescriptionTemplate.cs \
	Project/CodeBehind.cs \
	Project/VerifyCodeBehindBuildStep.cs \
	Project/XspParameters.cs

RES =  \
	AspNetAddIn.addin.xml \
	AspNetAddIn.glade \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	Templates/MasterPage-CodeBehind.xft.xml \
	Templates/MasterPage-Empty.xft.xml \
	Templates/WebApplication.xpt.xml \
	Templates/WebControl-CodeBehind.xft.xml \
	Templates/WebControl-Empty.xft.xml \
	Templates/WebForm-CodeBehind.xft.xml \
	Templates/WebForm-Empty.xft.xml \
	Templates/WebHandler-CodeBehind.xft.xml \
	Templates/WebHandler-Empty.xft.xml \
	Templates/WebService-CodeBehind.xft.xml \
	Templates/WebService-Empty.xft.xml
	
TESTFILES = \
	Test/Test.cs

TESTDLLS = \
	$(NUNIT_LIBS)

TESTASSEMBLY = $(top_builddir)/build/bin/AspNetAddIn.Test.dll


if ENABLE_ASPNET
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

check: all $(TESTASSEMBLY)	
	MONO_PATH=../../build/AddIns nunit-console $(TESTASSEMBLY)

$(TESTASSEMBLY) : $(build_test_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -debug -codepage:utf8 -out:$@ -target:library $(DLLS) $(TESTDLLS) /r:$(ASSEMBLY) \
	$(monodoc_libflags) $(build_resources:%=/resource:%) $(build_test_sources)

if ENABLE_ASPNET
csharpbindinglibdir = $(MD_ADDIN_DIR)/AspNetAddIn
csharpbindinglib_DATA = $(ASSEMBLY)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

