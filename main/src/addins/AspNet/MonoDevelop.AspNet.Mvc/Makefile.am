ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.AspNet.Mvc
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.AspNet.Mvc.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.AspNet/MonoDevelop.AspNet.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.TextTemplating/Mono.TextTemplating.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.TextTemplating/MonoDevelop.TextTemplating.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.XmlEditor/MonoDevelop.XmlEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml

FILES =  \
	AspMvcCommands.cs \
	AspMvcProject.cs \
	AspMvcProjectBinding.cs \
	AspMvcProjectConfiguration.cs \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.AspNet.Mvc.Gui.AddViewDialog.cs \
	Gui/AddViewDialog.cs \
	Gui/FolderNodeBuilderExtension.cs \
	TextTemplating/MvcTextTemplateHost.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.AspNet.Mvc.addin.xml

DATA_FILES = 

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/Schemas/, $(notdir $(DATA_FILES)))

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)/Schemas
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $@

check: all

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.AspNet.Mvc
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

schemadir = $(MD_ADDIN_DIR)/MonoDevelop.AspNet.Mvc/Schemas
schema_DATA = $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include

