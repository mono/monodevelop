
ASSEMBLY = $(top_builddir)/build/AddIns/MonoDevelop.Documentation.dll

REFS =  \
	$(MONODOC_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:System.Xml

FILES = MonoDevelop.Documentation/DocumentationService.cs

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

ADDIN = MonoDevelop.Documentation.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(build_sources)

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(ADDIN)

include $(top_srcdir)/Makefile.include
