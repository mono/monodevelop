
DLL = $(top_builddir)/build/AddIns/MonoDevelop.Documentation.dll

REFERENCES = \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	$(MONODOC_LIBS)

FILES= \
MonoDevelop.Documentation/MonodocService.cs

GENERATED_FILES = \
AssemblyInfo.cs

all: $(DLL) $(ADDIN_BUILD)/$(ADDIN)

ADDIN = MonoDevelop.Documentation.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(DLL): $(build_sources)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFERENCES) $(build_sources)

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(DLL)

CLEANFILES = $(DLL) $(DLL).mdb

EXTRA_DIST = $(FILES) $(ADDIN)

include $(top_srcdir)/Makefile.include
