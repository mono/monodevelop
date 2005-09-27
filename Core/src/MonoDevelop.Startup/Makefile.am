
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.exe

FILES= \
MonoDevelop.Startup/MonoDevelopMain.cs \
MonoDevelop.Startup/MonoDevelopOptions.cs \
MonoDevelop.Startup/AddInTreeSettingsHandler.cs \
MonoDevelop.Startup/ExceptionBox.cs \
MonoDevelop.Startup/AddinLoadErrorDialog.cs

RES= \
SplashScreen.png \
MonoDevelop.Startup.glade

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY $(ASSEMBLY).config

REFS = \
	/r:System.Drawing \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	/r:Mono.Posix.dll \
	/r:Mono.GetOptions.dll \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS)

$(ASSEMBLY): $(build_sources) $(RES)
	$(CSC) $(CSC_FLAGS) -out:$@ $(REFS) $(RES:%=/resource:%) $(build_sources)

$(ASSEMBLY).config: $(ASSEMBLY) $(srcdir)/app.config
	cp $(srcdir)/app.config $(ASSEMBLY).config

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).config

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES) app.config

include $(top_srcdir)/Makefile.include

