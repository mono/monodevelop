
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/JavaBinding.dll
REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	gtk-gui/generated.cs \
	gtk-gui/JavaBinding.CodeGenerationPanelWidget.cs \
	gtk-gui/JavaBinding.GlobalOptionsPanelWidget.cs \
	Gui/GlobalOptionsPanel.cs \
	Gui/ProjectConfigurationPropertyPanel.cs \
	IKVMCompilerManager.cs \
	JavaCompiler.cs \
	JavaLanguageBinding.cs \
	Project/JavaCompilerParameters.cs

RES =  \
	gtk-gui/gui.stetic \
	icons/Java.FileIcon \
	icons/java-icon-32.png \
	JavaBinding.addin.xml \
	templates/EmptyJavaFile.xft.xml \
	templates/EmptyJavaProject.xpt.xml \
	templates/IkvmConsoleApplicationProject.xpt.xml \
	templates/IkvmGladeApplicationProject.xpt.xml \
	templates/IkvmGnomeApplicationProject.xpt.xml \
	templates/IkvmGtkApplicationProject.xpt.xml \
	templates/IkvmLibraryProject.xpt.xml \
	templates/JavaApplet.xft.xml \
	templates/JavaApplication.xft.xml \
	templates/JavaApplicationProject.xpt.xml \
	templates/JavaConsoleApplicationProject.xpt.xml \
	templates/JavaDialog.xft.xml \
	templates/JavaFrame.xft.xml \
	templates/JavaOKDialog.xft.xml \
	templates/JavaPanel.xft.xml

if ENABLE_JAVA
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

if ENABLE_JAVA
assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY)

endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
