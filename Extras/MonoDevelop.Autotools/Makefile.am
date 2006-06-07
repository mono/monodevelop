
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Autotools
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Autotools.dll

REFS = -r:Mono.Posix \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/log4net.dll  \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll  \
	$(GTK_SHARP_LIBS) 

FILES = \
	AssemblyInfo.cs		\
	AutotoolsContext.cs	\
	DeployHandler.cs	\
	IMakefileHandler.cs	\
	ISimpleAutotoolsSetup.cs \
	Makefile.cs	\
	Set.cs				\
	SimpleProjectMakefileHandler.cs	\
	SolutionDeployer.cs	\
	SolutionMakefileHandler.cs	\
	TemplateEngine.cs

RES =

DATA_FILES = \
	templates/Makefile.am.project.template	\
	templates/Makefile.include	\
	templates/autogen.sh.template	\
	templates/configure.ac.template	\
	templates/exe.wrapper.in.template

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(DATA_FILES))

ADDIN = MonoDevelop.Autotools.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(DATA_FILE_BUILD)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(DATA_FILE_BUILD): $(srcdir)/$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(RES:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Autotools
assembly_DATA = $(ASSEMBLY) $(ADDIN) $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD) $(ADDIN_BUILD)/$(ADDIN)
EXTRA_DIST = $(FILES) $(ADDIN) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
