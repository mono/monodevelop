FILES = \
MonoTestProvider.cs

REFS = -r:System \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/NUnit/MonoDevelop.NUnit.dll \
	-r:$(top_builddir)/build/AddIns/MonoDeveloperExtensions/MonoDeveloperExtensions.dll

ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDeveloperExtensions

ASSEMBLY = $(ADDIN_BUILD)/MonoDeveloperExtensions_nunit.dll

if ENABLE_MONOEXTENSIONS
all: $(ASSEMBLY)
else
all:
endif

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include

if ENABLE_MONOEXTENSIONS
MonoDeveloperExtensionsdir = $(MD_ADDIN_DIR)/MonoDeveloperExtensions
MonoDeveloperExtensions_DATA = $(ASSEMBLY)
endif
