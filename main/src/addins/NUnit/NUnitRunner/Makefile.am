FILES = NUnitTestRunner.cs

REFS =  \
	-r:$(top_srcdir)/src/addins/NUnit/lib/nunit.core.dll \
	-r:$(top_srcdir)/src/addins/NUnit/lib/nunit.core.interfaces.dll \
	-r:$(top_srcdir)/src/addins/NUnit/lib/nunit.framework.dll \
	-r:System \
	-r:System.Xml

ADDIN_BUILD = $(top_builddir)/build/AddIns/NUnit
ASSEMBLY = $(ADDIN_BUILD)/NUnitRunner.dll

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS) $(build_deps)

$(ASSEMBLY).mdb: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include

assemblydir = $(MD_ADDIN_DIR)/NUnit
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
