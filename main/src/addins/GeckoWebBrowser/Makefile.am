ADDIN_BUILD = $(top_builddir)/build/AddIns/GeckoWebBrowser
ASSEMBLY = $(ADDIN_BUILD)/GeckoWebBrowser.dll

REFS =  \
	$(GECKO_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:Mono.Posix

FILES =  \
	GeckoWebBrowser.cs \
	GeckoWebBrowserLoader.cs \
	gtk-gui/generated.cs  

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.WebBrowsers.GeckoWebBrowser.addin.xml  

if ENABLE_GECKOSHARP
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

if ENABLE_GECKOSHARP
assemblydir = $(MD_ADDIN_DIR)/GeckoWebBrowser
assembly_DATA = $(ASSEMBLY)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
