
ASSEMBLY = $(top_builddir)/build/AddIns/MonoDevelop.Documentation.dll

REFS =  \
	$(MONODOC_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:System.Xml

FILES = MonoDevelop.Documentation/DocumentationService.cs

RES = MonoDevelop.Documentation.addin.xml

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY)

ADDIN_BUILD = $(top_builddir)/build/AddIns

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
