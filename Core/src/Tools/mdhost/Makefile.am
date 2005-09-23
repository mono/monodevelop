
ASSEMBLY = mdhost.exe

FILES = src/AssemblyInfo.cs \
src/mdhost.cs

build_sources = $(addprefix $(srcdir)/, $(FILES))

all: $(ASSEMBLY)

REFS = /r:$(top_builddir)/build/bin/MonoDevelop.Base.dll /r:System.Runtime.Remoting
	
$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) /out:$(ASSEMBLY) \
	&& cp $(ASSEMBLY) $(top_builddir)/build/bin/.

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)           

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

