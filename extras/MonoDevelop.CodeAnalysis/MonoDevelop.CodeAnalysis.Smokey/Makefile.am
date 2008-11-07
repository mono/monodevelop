ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CodeAnalysis.Smokey.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml \
	-r:$(top_builddir)/build/MonoDevelop.CodeAnalysis.dll \
	-r:$(top_srcdir)/dlls/smokey.exe

FILES =  \
	gtk-gui/generated.cs \
	SmokeyRule.cs \
	SmokeyRuleLoader.cs \
	SmokeyRunner.cs \
	SmokeyViolation.cs \
	SmokeyParser.cs \
	SmokeyRuleCache.cs \
	SmokeyAnalyzer.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.CodeAnalysis.Smokey.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ADDIN_BUILD)/smokey.exe: $(top_srcdir)/dlls/smokey.exe
	cp $^ $@

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.CodeAnalysis
assembly_DATA = $(ASSEMBLY) $(ADDIN_BUILD)/smokey.exe

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
