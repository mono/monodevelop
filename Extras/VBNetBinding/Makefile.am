
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/VBNetBinding.dll

SUBDIRS = SharpRefactoryVB

DLLS = /r:System.Drawing.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.SourceEditor.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:$(top_builddir)/build/bin/ICSharpCode.SharpRefactory.dll \
	/r:$(top_builddir)/build/bin/ICSharpCode.SharpRefactory.VB.dll \
	/r:$(top_builddir)/build/bin/ICSharpCode.SharpAssembly.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
AssemblyInfo.cs \
VBAmbience.cs \
VBBindingCompilerServices.cs \
VBLanguageBinding.cs \
Gui/CodeGenerationPanel.cs \
Parser/ExpressionFinder.cs \
Parser/Parser.cs \
Parser/Resolver.cs \
Parser/TypeVisitor.cs \
Parser/VBNetVisitor.cs \
Parser/SharpDevelopTree/AttributeSection.cs \
Parser/SharpDevelopTree/Class.cs \
Parser/SharpDevelopTree/CompilationUnit.cs \
Parser/SharpDevelopTree/Constructor.cs \
Parser/SharpDevelopTree/Event.cs \
Parser/SharpDevelopTree/Field.cs \
Parser/SharpDevelopTree/Indexer.cs \
Parser/SharpDevelopTree/Method.cs \
Parser/SharpDevelopTree/Parameter.cs \
Parser/SharpDevelopTree/Property.cs \
Parser/SharpDevelopTree/ReturnType.cs \
Project/VBCompilerParameters.cs

RES = \
VB.glade \
templates/EmptyVBFile.xft.xml \
templates/EmptyVBProject.xpt.xml \
templates/VBConsoleApplicationProject.xpt.xml \
templates/VBGtkSharpProject.xpt.xml \
templates/VBGtkSharp2Project.xpt.xml \
icons/VB.File.EmptyFile \
icons/VB.File.Form \
icons/VB.File.FullFile \
icons/VB.FileIcon \
icons/VB.File.NewClass \
icons/VB.File.WebFile \
icons/VB.Project.DOSProject \
icons/VB.Project.EmptyProject \
icons/VB.Project.Form \
icons/VB.Project.FullProject \
icons/VB.ProjectIcon \
icons/VB.Project.Library \
icons/VB.Project.ServiceProject \
icons/VB.Project.UserControl \
icons/VB.Project.WebProject

ADDIN = VBNetBinding.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	cd SharpRefactoryVB && $(MAKE)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(DLLS) \
		$(RES:%=/resource:%) \
		$(build_sources)

vbnetbindinglibdir = $(MD_ADDIN_DIR)/BackendBindings
vbnetbindinglib_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

DISTCLEANFILES =

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)
include $(top_srcdir)/Makefile.include

