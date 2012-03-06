
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Deployment
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Deployment.Linux.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml 

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Deployment.Linux.BasicOptionPanelWidget.cs \
	gtk-gui/MonoDevelop.Deployment.Linux.DesktopPanelWidget.cs \
	gtk-gui/MonoDevelop.Deployment.Linux.DotDesktopViewWidget.cs \
	gtk-gui/MonoDevelop.Deployment.Linux.MenuCategorySelectorDialog.cs \
	MonoDevelop.Deployment.Linux/BasicOptionPanel.cs \
	MonoDevelop.Deployment.Linux/BasicOptionPanelWidget.cs \
	MonoDevelop.Deployment.Linux/DesktopEntry.cs \
	MonoDevelop.Deployment.Linux/DesktopPanelWidget.cs \
	MonoDevelop.Deployment.Linux/DotDesktopDisplayBinding.cs \
	MonoDevelop.Deployment.Linux/DotDesktopView.cs \
	MonoDevelop.Deployment.Linux/DotDesktopViewWidget.cs \
	MonoDevelop.Deployment.Linux/LinuxDeployData.cs \
	MonoDevelop.Deployment.Linux/LinuxDeployExtension.cs \
	MonoDevelop.Deployment.Linux/LinuxDeploymentSetup.cs \
	MonoDevelop.Deployment.Linux/LinuxIntegrationProjectFeature.cs \
	MonoDevelop.Deployment.Linux/LinuxTargetDirectory.cs \
	MonoDevelop.Deployment.Linux/MenuCategorySelectorDialog.cs

RES =  \
	app.desktop.xft.xml \
	gtk-gui/gui.stetic \
	MonoDevelop.Deployment.Linux.addin.xml \
	MonoDevelop.Deployment.Linux/DesktopInfo.xml

DATA_FILES = 

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(DATA_FILES))

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)/$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Deployment
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
