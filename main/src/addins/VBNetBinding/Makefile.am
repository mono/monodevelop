
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.VBNetBinding.dll

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/NRefactory.dll 
	
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.VBNetBinding.ConfigurationOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.VBNetBinding.ImportsOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.VBNetBinding.ProjectOptionsPanelWidget.cs \
	Gui/ConfigurationOptionsPanel.cs \
	Gui/ConfigurationOptionsPanelWidget.cs \
	Gui/ImportsOptionsPanel.cs \
	Gui/ImportsOptionsPanelWidget.cs \
	Gui/ProjectOptionsPanel.cs \
	Gui/ProjectOptionsPanelWidget.cs \
	Parser/DomParser.cs \
	Project/Import.cs \
	Project/VBCompilerParameters.cs \
	Project/VBNetResourceIdBuilder.cs \
	Project/VBProjectParameters.cs \
	VBBindingCompilerServices.cs \
	VBCodeGenerator.cs \
	VBLanguageBinding.cs

RES =  \
	gtk-gui/gui.stetic \
	icons/VB.FileIcon \
	icons/vb-icon-32.png \
	md1format.xml \
	templates/AssemblyInfo.xft.xml \
	templates/EmptyVBFile.xft.xml \
	templates/EmptyVBProject.xpt.xml \
	templates/VBConsoleApplicationProject.xpt.xml \
	templates/VBGtkSharp2Project.xpt.xml \
	VBNetBinding.addin.xml

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) \
		$(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

vbnetbindinglibdir = $(MD_ADDIN_DIR)/BackendBindings
vbnetbindinglib_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

DISTCLEANFILES =

EXTRA_DIST = $(FILES) $(RES)
include $(top_srcdir)/Makefile.include

