
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Autotools
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Autotools.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System 

FILES =  \
	AssemblyInfo.cs \
	AutotoolsContext.cs \
	Commands.cs \
	ConfigSection.cs \
	CustomMakefile.cs \
	FileNodeBuilderExtension.cs \
	GenerateMakefilesDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Autotools.GenerateMakefilesDialog.cs \
	gtk-gui/MonoDevelop.Autotools.MakefileIntegrationFeatureWidget.cs \
	gtk-gui/MonoDevelop.Autotools.MakefileOptionPanelWidget.cs \
	gtk-gui/MonoDevelop.Autotools.MakefileSwitchEditor.cs \
	gtk-gui/MonoDevelop.Autotools.MakefileSwitchEditorWidget.cs \
	gtk-gui/MonoDevelop.Autotools.TarballBuilderEditorWidget.cs \
	Handler.cs \
	IMakefileHandler.cs \
	ImportMakefileDialog.cs \
	ISimpleAutotoolsSetup.cs \
	Makefile.cs \
	MakefileData.cs \
	MakefileGeneratorTool.cs \
	MakefileIntegrationFeature.cs \
	MakefileIntegrationFeatureWidget.cs \
	MakefileOptionPanel.cs \
	MakefileOptionPanelWidget.cs \
	MakefileProject.cs \
	MakefileProjectServiceExtension.cs \
	MakefileSwitchEditor.cs \
	MakefileSwitchEditorWidget.cs \
	MakefileVar.cs \
	PropertyProvider.cs \
	Set.cs \
	SimpleProjectMakefileHandler.cs \
	SolutionDeployer.cs \
	SolutionMakefileHandler.cs \
	Switch.cs \
	TarballBuilderEditorWidget.cs \
	TemplateEngine.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Autotools.addin.xml

DATA_FILES =  \
	templates/autogen.sh.template \
	templates/configure.ac.template \
	templates/configure.template \
	templates/exe.wrapper.in.template \
	templates/expansions.m4 \
	templates/make-dist.targets \
	templates/Makefile.am.project.template \
	templates/Makefile.include \
	templates/Makefile.noauto.project.template \
	templates/Makefile.solution.template \
	templates/package.pc.template \
	templates/rules.make

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/templates/, $(notdir $(DATA_FILES)))

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)/templates
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Autotools
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILES)

templatedir = $(assemblydir)/templates
template_DATA = $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
