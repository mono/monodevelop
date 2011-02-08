
ASSEMBLY = $(top_builddir)/build/bin/mdhost.exe

FILES = src/AssemblyInfo.cs \
src/mdhost.cs

all: $(ASSEMBLY)

REFS = -r:System.Runtime.Remoting \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix

DEPS = $(top_builddir)/build/bin/MonoDevelop.Core.dll
	
$(ASSEMBLY): $(build_sources) $(DEPS)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) $(build_deps) -out:$(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb           

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include
