
ADDIN_BUILD = $(top_builddir)/build/AddIns
ASSEMBLY = $(ADDIN_BUILD)/MacPlatform.dll

DEPS =	\
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll

REFS =  \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:System

FILES =  \
	AssemblyInfo.cs \
	MacPlatform.cs

RES =  \
	MacPlatform.addin.xml

if ENABLE_MACPLATFORM
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

if ENABLE_MACPLATFORM
assemblydir = $(MD_ADDIN_DIR)/MacPlatform
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
