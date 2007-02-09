
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.VersionControl.Subversion.dll
ADDIN = MonoDevelop.VersionControl.Subversion.addin.xml

REFS = 	/r:Mono.Posix.dll \
       $(GTK_SHARP_LIBS) \
	   /r:$(ADDIN_BUILD)/VersionControlAddIn.dll \
	   /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll

FILES = MonoDevelop.VersionControl.Subversion/SubversionRepository.cs \
		MonoDevelop.VersionControl.Subversion/SvnClient.cs \
		MonoDevelop.VersionControl.Subversion/SubversionVersionControl.cs \
		MonoDevelop.VersionControl.Subversion/LibApr.cs \
		gtk-gui/generated.cs

RES = gtk-gui/objects.xml

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) --unsafe $(build_sources) $(REFS) $(build_resources:%=/resource:%) -out:$@ -target:library

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN)
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

if ENABLE_SUBVERSION
all: $(ADDIN_BUILD)/$(ADDIN) $(ASSEMBLY)
else
all:
endif

if ENABLE_SUBVERSION
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(srcdir)/$(ADDIN) $(ASSEMBLY)
endif

include $(top_srcdir)/Makefile.include
