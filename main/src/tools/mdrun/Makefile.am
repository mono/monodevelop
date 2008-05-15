
ASSEMBLY = $(top_builddir)/build/bin/mdrun.exe

FILES = src/AssemblyInfo.cs \
src/mdrun.cs

all: $(ASSEMBLY) $(ASSEMBLY).config

REFS = -r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(MONO_ADDINS_LIBS) \
	$(MONO_ADDINS_SETUP_LIBS)
	
$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) /out:$(ASSEMBLY)

$(ASSEMBLY).config: $(ASSEMBLY) $(srcdir)/app.config
	cp $(srcdir)/app.config $(ASSEMBLY).config

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) app.config

include $(top_srcdir)/Makefile.include
