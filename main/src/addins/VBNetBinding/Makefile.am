
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.VBNetBinding.dll

DEPS =	\
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	$(top_builddir)/build/bin/NRefactory.dll

REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/NRefactory.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.VBNetBinding.CodeGenerationPanelWidget.cs \
	Gui/CodeGenerationPanel.cs \
	Gui/CodeGenerationPanelWidget.cs \
	Parser/DomParser.cs \
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
	Project/VBNetResourceIdBuilder.cs \
	VBAmbience.cs \
	VBBindingCompilerServices.cs \
	VBCodeGenerator.cs \
	VBLanguageBinding.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/VB.FileIcon \
	icons/vb-icon-32.png \
	md1format.xml \
	templates/AssemblyInfo.xft.xml \
	templates/EmptyVBFile.xft.xml \
	templates/EmptyVBProject.xpt.xml \
	templates/VBConsoleApplicationProject.xpt.xml \
	templates/VBGtkSharp2Project.xpt.xml \
	VBNetBinding.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) \
		$(build_sources)

vbnetbindinglibdir = $(MD_ADDIN_DIR)/BackendBindings
vbnetbindinglib_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

DISTCLEANFILES =

EXTRA_DIST = $(FILES) $(RES)
include $(top_srcdir)/Makefile.include

