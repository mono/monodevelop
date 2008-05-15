
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.exe

FILES = MonoDevelop.Startup/MonoDevelopMain.cs

RES =            

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY) $(ASSEMBLY).config

REFS =  \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll

$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -out:$@ $(REFS) $(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).config: $(ASSEMBLY) $(srcdir)/app.config
	cp $(srcdir)/app.config $(ASSEMBLY).config

$(ASSEMBLY).addins: $(ASSEMBLY) $(srcdir)/MonoDevelop.exe.addins
	cp $(srcdir)/MonoDevelop.exe.addins $(ASSEMBLY).addins
	
assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config $(ASSEMBLY).addins

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES) app.config MonoDevelop.exe.addins

include $(top_srcdir)/Makefile.include

