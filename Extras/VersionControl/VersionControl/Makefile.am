
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/VersionControl.dll

REFS = 	/r:Mono.Posix.dll \
       $(GTK_SHARP_LIBS) \
	   /r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	   /r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll

FILES = VersionControl.Service.Subversion/SubversionRepository.cs \
		VersionControl.Service.Subversion/SvnClient.cs \
		VersionControl.Service.Subversion/SubversionVersionControl.cs \
		VersionControl.Service.Subversion/LibApr.cs \
		VersionControl.Service.Cvs/CvsRepository.cs \
		VersionControl.Service.Cvs/CvsSystem.cs \
		VersionControl.Service/VersionControlSystem.cs \
		VersionControl.Service/VersionControlConfiguration.cs \
		VersionControl.Service/Revision.cs \
		VersionControl.Service/UnknownRepository.cs \
		VersionControl.Service/Repository.cs \
		VersionControl.Service/UrlBasedRepository.cs \
		VersionControl.Service/VersionInfo.cs \
		VersionControl.Service/VersionControlService.cs \
		VersionControl.Service/UrlBasedRepositoryEditor.cs \
		VersionControl.Service/VersionStatus.cs \
		gtk-gui/generated.cs

RES = gtk-gui/objects.xml

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) $(RES:%=/resource:%) -out:$@ -target:library

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

if ENABLE_VERSIONCONTROL
all: $(ASSEMBLY)
else
all:
endif

if ENABLE_VERSIONCONTROL
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(ASSEMBLY)
endif

include $(top_srcdir)/Makefile.include
