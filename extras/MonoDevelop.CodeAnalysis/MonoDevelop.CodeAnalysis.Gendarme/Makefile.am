ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CodeAnalysis.Gendarme.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:$(top_builddir)/build/MonoDevelop.CodeAnalysis.dll \
	-r:$(top_srcdir)/dlls/Gendarme.Framework.dll \
	-r:$(top_srcdir)/dlls/Mono.Cecil.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	GendarmeAnalyzer.cs \
	GendarmeRule.cs \
	GendarmeRuleCache.cs \
	GendarmeRuleLoader.cs \
	GendarmeRunner.cs \
	GendarmeViolation.cs \
	gtk-gui/generated.cs \
	Utilities.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.CodeAnalysis.Gendarme.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ADDIN_BUILD)/Gendarme.Framework.dll: $(top_srcdir)/dlls/Gendarme.Framework.dll
	cp $^ $@

$(ADDIN_BUILD)/Mono.Cecil.dll: $(top_srcdir)/dlls/Mono.Cecil.dll
	cp $^ $@

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.CodeAnalysis
assembly_DATA = $(ASSEMBLY) $(ADDIN_BUILD)/Mono.Cecil.dll $(ADDIN_BUILD)/Gendarme.Framework.dll

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
