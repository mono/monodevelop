FILES = MonoTestProvider.cs

REFS = -r:System

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDeveloperExtensions/MonoDeveloperExtensions.dll \
	$(top_builddir)/build/AddIns/NUnit/MonoDevelop.NUnit.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll

ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDeveloperExtensions

ASSEMBLY = $(ADDIN_BUILD)/MonoDeveloperExtensions_nunit.dll

if ENABLE_MONOEXTENSIONS
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

$(ASSEMBLY): $(build_sources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS) $(build_deps)

$(ASSEMBLY).mdb: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include

if ENABLE_MONOEXTENSIONS
MonoDeveloperExtensionsdir = $(MD_ADDIN_DIR)/MonoDeveloperExtensions
MonoDeveloperExtensions_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif
