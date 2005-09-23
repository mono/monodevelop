
ASSEMBLY = $(top_builddir)/build/bin/dbgen.exe

FILES = src/AssemblyInfo.cs \
src/dbgen.cs

all: $(ASSEMBLY)

REFS = /r:$(top_builddir)/build/bin/MonoDevelop.Base.dll /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll
	
$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) -out:$@

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)           

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)
include $(top_srcdir)/Makefile.include

