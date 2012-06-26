
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.VersionControl.Subversion.Unix.dll

DEPS =  \
	$(top_builddir)/build/AddIns/VersionControl/MonoDevelop.VersionControl.dll \
	$(top_builddir)/build/AddIns/VersionControl/MonoDevelop.VersionControl.Subversion.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	-r:../../../../build/AddIns/VersionControl/MonoDevelop.VersionControl.dll \
	-r:../../../../build/AddIns/VersionControl/MonoDevelop.VersionControl.Subversion.dll \
	-r:../../../../build/bin/MonoDevelop.Core.dll \
	-r:System

FILES =  \
	AssemblyInfo.cs \
	MonoDevelop.VersionControl.Subversion.Unix/LibApr.cs \
	MonoDevelop.VersionControl.Subversion.Unix/LibSvnClient.cs \
	MonoDevelop.VersionControl.Subversion.Unix/SvnClient.cs

ASM_CONFIG = MonoDevelop.VersionControl.Subversion.Unix.dll.config

RES = MonoDevelop.VersionControl.Subversion.Unix.addin.xml

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -unsafe $(build_sources) $(REFS) $(build_deps) $(build_resources:%=/resource:%) -out:$@ -target:library

$(ASSEMBLY).config: $(srcdir)/$(ASM_CONFIG)
	mkdir -p $(ADDIN_BUILD)
	cp $? $@

$(ASSEMBLY).mdb: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES) $(ASM_CONFIG)

if ENABLE_SUBVERSION
all: $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config
else
all:
endif

if ENABLE_SUBVERSION
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config
endif

include $(top_srcdir)/Makefile.include
