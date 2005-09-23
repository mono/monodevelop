
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.exe
SPLASH = -resource:$(srcdir)/SplashScreen.png,SplashScreen.png

FILES= \
MonoDevelopMain.cs \
MonoDevelopOptions.cs \
Dialogs/AddinLoadErrorDialog.cs \
Dialogs/AddInTreeSettingsHandler.cs

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY $(ASSEMBLY).config

REFS = \
	/r:System.Drawing \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:Mono.Posix.dll \
	/r:Mono.GetOptions.dll \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS)

RES = -resource:$(srcdir)/MonoDevelop.Startup.glade

$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) $(RES) -out:$@ $(REFS) $(build_sources) $(SPLASH)

$(ASSEMBLY).config: $(ASSEMBLY) $(srcdir)/app.config
	cp $(srcdir)/app.config $(ASSEMBLY).config

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).config

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) SplashScreen.png app.config MonoDevelop.Startup.glade

include $(top_srcdir)/Makefile.include

