FILES = CSharpAutotoolsSetup.cs

REFS =  \
	-r:$(top_builddir)/build/AddIns/BackendBindings/MonoDevelop.CSharpBinding.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Autotools/MonoDevelop.Autotools.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:System

ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CSharpBinding.Autotools.dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(build_sources) $(REFS)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include

csharpbindinglibdir = $(MD_ADDIN_DIR)/BackendBindings
csharpbindinglib_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
