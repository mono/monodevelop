
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.exe

FILES = MonoDevelop.Startup/MonoDevelopMain.cs

RES =   

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY) $(ASSEMBLY).config

REFS = -r:$(top_builddir)/build/bin/MonoDevelop.Core.dll

$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -out:$@ $(REFS) $(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).config: $(ASSEMBLY) $(srcdir)/app.config
	cp $(srcdir)/app.config $(ASSEMBLY).config

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).config

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES) app.config

include $(top_srcdir)/Makefile.include

