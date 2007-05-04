
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/VBNetBinding.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/NRefactory.dll \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	Gui/CodeGenerationPanel.cs \
	Parser/ExpressionFinder.cs \
	Parser/Parser.cs \
	Parser/Resolver.cs \
	Parser/SharpDevelopTree/Class.cs \
	Parser/SharpDevelopTree/CompilationUnit.cs \
	Parser/SharpDevelopTree/Constructor.cs \
	Parser/SharpDevelopTree/ReturnType.cs \
	Parser/TypeVisitor.cs \
	Parser/VBNetVisitor.cs \
	Project/VBCompilerParameters.cs \
	VBAmbience.cs \
	VBBindingCompilerServices.cs \
	VBLanguageBinding.cs

RES =  \
	icons/VB.File.EmptyFile \
	icons/VB.File.Form \
	icons/VB.File.FullFile \
	icons/VB.File.NewClass \
	icons/VB.File.WebFile \
	icons/VB.FileIcon \
	icons/VB.Project.DOSProject \
	icons/VB.Project.EmptyProject \
	icons/VB.Project.Form \
	icons/VB.Project.FullProject \
	icons/VB.Project.Library \
	icons/VB.Project.ServiceProject \
	icons/VB.Project.UserControl \
	icons/VB.Project.WebProject \
	icons/VB.ProjectIcon \
	templates/EmptyVBFile.xft.xml \
	templates/EmptyVBProject.xpt.xml \
	templates/VBConsoleApplicationProject.xpt.xml \
	templates/VBGtkSharp2Project.xpt.xml \
	templates/VBGtkSharpProject.xpt.xml \
	VB.glade

ADDIN = VBNetBinding.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) \
		$(build_sources)

vbnetbindinglibdir = $(MD_ADDIN_DIR)/BackendBindings
vbnetbindinglib_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

DISTCLEANFILES =

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)
include $(top_srcdir)/Makefile.include

