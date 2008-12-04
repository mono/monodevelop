
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.exe

FILES =  \
	AssemblyInfo.cs \
	MonoDevelop.Startup/MonoDevelopMain.cs

RES =

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS = $(MONO_ADDINS_LIBS)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	$(CSC) $(CSC_FLAGS) -out:$@ $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).config: $(ASSEMBLY) $(srcdir)/app.config
	cp $(srcdir)/app.config $(ASSEMBLY).config

$(ASSEMBLY).addins: $(ASSEMBLY) $(srcdir)/MonoDevelop.exe.addins
	cp $(srcdir)/MonoDevelop.exe.addins $(ASSEMBLY).addins
	
$(ASSEMBLY).mdb: $(ASSEMBLY)
	
assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config $(ASSEMBLY).addins

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES) app.config MonoDevelop.exe.addins

include $(top_srcdir)/Makefile.include

