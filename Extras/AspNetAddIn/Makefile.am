ADDIN_BUILD = $(top_builddir)/build/AddIns/AspNetAddIn
ASSEMBLY = $(ADDIN_BUILD)/AspNetAddIn.dll

REFS = -r:System \
       -r:System.Xml \
       -r:System.Web \
       -r:System.Design \
       -r:System.Drawing \
       -r:System.Drawing.Design \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll  \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(GTK_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
	Project/AspNetAppProject.cs \
	Project/AspNetAppProjectBinding.cs \
	Project/AspNetAppProjectConfiguration.cs \
	Project/AspNetFileDescriptionTemplate.cs \
	Project/AspNetCodeBehindProvider.cs \
	Project/AspNetDisplayBinding.cs \
	Project/XspParameters.cs \
	Project/VerifyCodeBehindBuildStep.cs \
	\
	Gui/XspOptionsPanel.cs \
	\
	Parser/Document.cs \
	Parser/MemberListVisitor.cs \
	Parser/WebFormReferenceManager.cs \
	Parser/PageInfoVisitor.cs \
	Parser/Internal/AspParser.cs \
	Parser/Internal/AspTokenizer.cs \
	Parser/Internal/Directive.cs \
	Parser/Internal/Location.cs \
	Parser/Internal/ParseException.cs \
	Parser/Internal/StrUtils.cs \
	Parser/Internal/TagAttributes.cs \
	Parser/Internal/TagType.cs \
	Parser/Tree/CodeRenderNode.cs \
	Parser/Tree/DirectiveNode.cs \
	Parser/Tree/ILocation.cs \
	Parser/Tree/RootNode.cs \
	Parser/Tree/Node.cs \
	Parser/Tree/ParentNode.cs \
	Parser/Tree/ServerIncludeNode.cs \
	Parser/Tree/ServerCommentNode.cs \
	Parser/Tree/TagNode.cs \
	Parser/Tree/TextNode.cs \
	Parser/Tree/Visitor.cs

RES = \
	Templates/EmptyWebHandler.xft.xml \
	Templates/EmptyWebForm.xft.xml \
	Templates/EmptyWebControl.xft.xml \
	Templates/EmptyWebService.xft.xml \
	Templates/CodeBehindWebHandler.xft.xml \
	Templates/CodeBehindWebForm.xft.xml \
	Templates/CodeBehindWebControl.xft.xml \
	Templates/CodeBehindWebService.xft.xml \
	Templates/WebApplication.xpt.xml \
	AspNetAddIn.glade
	

ADDIN = AspNetAddIn.addin.xml

TESTFILES = \
	Test/Test.cs

TESTDLLS = \
	$(NUNIT_LIBS)

TESTASSEMBLY = $(top_builddir)/build/bin/AspNetAddIn.Test.dll


if ENABLE_ASPNET
all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(RES:%=/resource:%) $(build_sources)

check: all $(TESTASSEMBLY)	
	MONO_PATH=../../build/AddIns nunit-console $(TESTASSEMBLY)

$(TESTASSEMBLY) : $(build_test_sources) $(RES)
	$(CSC) $(CSC_FLAGS) -debug -codepage:utf8 -out:$@ -target:library $(DLLS) $(TESTDLLS) /r:$(ASSEMBLY) \
	$(monodoc_libflags) $(RES:%=/resource:%) $(build_test_sources)

if ENABLE_ASPNET
csharpbindinglibdir = $(MD_ADDIN_DIR)/AspNetAddIn
csharpbindinglib_DATA = $(ASSEMBLY) $(ADDIN)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include

