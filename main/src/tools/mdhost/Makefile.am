
ASSEMBLY = $(top_builddir)/build/bin/mdhost.exe

FILES = src/AssemblyInfo.cs \
src/mdhost.cs

all: $(ASSEMBLY)

REFS = -r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:System.Runtime.Remoting \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix
	
$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) -out:$(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb           

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include
