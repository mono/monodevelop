
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Autotools
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Autotools.dll

REFS =  \
	$(GNOME_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Deployment.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/contrib/log4net.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:Mono.Posix \
	-r:System 

FILES =  \
	AssemblyInfo.cs \
	AutotoolsContext.cs \
	Commands.cs \
	CustomMakefile.cs \
	FileNodeBuilderExtension.cs \
	GenerateMakefilesDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Autotools.GenerateMakefilesDialog.cs \
	gtk-gui/MonoDevelop.Autotools.MakefileIntegrationFeatureWidget.cs \
	gtk-gui/MonoDevelop.Autotools.MakefileOptionPanelWidget.cs \
	gtk-gui/MonoDevelop.Autotools.TarballBuilderEditorWidget.cs \
	Handler.cs \
	IMakefileHandler.cs \
	ImportMakefileDialog.cs \
	ISimpleAutotoolsSetup.cs \
	Makefile.cs \
	MakefileData.cs \
	MakefileIntegrationFeature.cs \
	MakefileIntegrationFeatureWidget.cs \
	MakefileOptionPanel.cs \
	MakefileOptionPanelWidget.cs \
	MakefileProject.cs \
	MakefileProjectServiceExtension.cs \
	MakefileReaderExtension.cs \
	MakefileVar.cs \
	PropertyProvider.cs \
	Set.cs \
	SimpleProjectMakefileHandler.cs \
	SolutionDeployer.cs \
	SolutionMakefileHandler.cs \
	TarballBuilderEditorWidget.cs \
	TemplateEngine.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml

DATA_FILES =  \
	templates/autogen.sh.template \
	templates/configure.ac.template \
	templates/configure.template \
	templates/exe.wrapper.in.template \
	templates/Makefile.am.project.template \
	templates/Makefile.include \
	templates/Makefile.noauto.project.template \
	templates/Makefile.solution.template \
	templates/package.pc.template \
	templates/rules.make

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(notdir $(DATA_FILES)))

ADDIN = MonoDevelop.Autotools.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(DATA_FILE_BUILD)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/templates$(subst $(ADDIN_BUILD),,$@) $@

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Autotools
assembly_DATA = $(ASSEMBLY) $(ADDIN) $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD) $(ADDIN_BUILD)/$(ADDIN)
EXTRA_DIST = $(FILES) $(ADDIN) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
