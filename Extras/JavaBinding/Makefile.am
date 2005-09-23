
ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/JavaBinding.dll

DLLS = /r:System.Drawing.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.SourceEditor.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
	$(GTK_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
Gui/ProjectConfigurationPropertyPanel.cs \
Gui/GlobalOptionsPanel.cs \
Project/JavaCompilerParameters.cs \
JavaBindingCompilerServices.cs \
JavaCompiler.cs \
JavaLanguageBinding.cs

RES = \
Java.glade \
templates/EmptyJavaFile.xft.xml \
templates/EmptyJavaProject.xpt.xml \
templates/IkvmConsoleApplicationProject.xpt.xml \
templates/IkvmGladeApplicationProject.xpt.xml \
templates/IkvmGnomeApplicationProject.xpt.xml \
templates/IkvmGtkApplicationProject.xpt.xml \
templates/IkvmLibraryProject.xpt.xml \
templates/JavaApplet.xft.xml \
templates/JavaApplicationProject.xpt.xml \
templates/JavaApplication.xft.xml \
templates/JavaConsoleApplicationProject.xpt.xml \
templates/JavaDialog.xft.xml \
templates/JavaFrame.xft.xml \
templates/JavaOKDialog.xft.xml \
templates/JavaPanel.xft.xml \
icons/Java.File.EmptyFile \
icons/Java.File.Form \
icons/Java.File.FullFile \
icons/Java.FileIcon \
icons/Java.File.NewClass \
icons/Java.File.WebFile \
icons/Java.Project.DOSProject \
icons/Java.Project.EmptyProject \
icons/Java.Project.Form \
icons/Java.Project.FullProject \
icons/Java.ProjectIcon \
icons/Java.Project.Library \
icons/Java.Project.ServiceProject \
icons/Java.Project.UserControl \
icons/Java.Project.WebProject

ADDIN = JavaBinding.addin.xml

if ENABLE_JAVA
all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(DLLS) $(RES:%=/resource:%) $(build_sources) -out:$@ -target:library

if ENABLE_JAVA
assemblydir = $(MD_ADDIN_DIR)/BackendBindings
assembly_DATA = $(ASSEMBLY) $(ADDIN)

endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include
