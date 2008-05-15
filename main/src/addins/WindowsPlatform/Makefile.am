
ADDIN_BUILD = $(top_builddir)/build/AddIns
ASSEMBLY = $(ADDIN_BUILD)/WindowsPlatform.dll
REFS =  \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:System

FILES =  \
	WindowsPlatform.cs

RES =  \
	WindowsPlatform.addin.xml

if ENABLE_WINDOWSPLATFORM
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(FILES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

if ENABLE_WINDOWSPLATFORM
assemblydir = $(MD_ADDIN_DIR)/WindowsPlatform
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
