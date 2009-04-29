
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.VersionControl.Subversion.dll

DEPS =  \
	$(top_builddir)/build/AddIns/VersionControl/MonoDevelop.VersionControl.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Posix \
	-r:System

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.VersionControl.Subversion.Gui.ClientCertificateDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Subversion.Gui.ClientCertificatePasswordDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Subversion.Gui.SslServerTrustDialog.cs \
	gtk-gui/MonoDevelop.VersionControl.Subversion.Gui.UserPasswordDialog.cs \
	MonoDevelop.VersionControl.Subversion.Gui/ClientCertificateDialog.cs \
	MonoDevelop.VersionControl.Subversion.Gui/ClientCertificatePasswordDialog.cs \
	MonoDevelop.VersionControl.Subversion.Gui/SslServerTrustDialog.cs \
	MonoDevelop.VersionControl.Subversion.Gui/UserPasswordDialog.cs \
	MonoDevelop.VersionControl.Subversion/Commands.cs \
	MonoDevelop.VersionControl.Subversion/ISvnClient.cs \
	MonoDevelop.VersionControl.Subversion/LibApr.cs \
	MonoDevelop.VersionControl.Subversion/LibSvnClient.cs \
	MonoDevelop.VersionControl.Subversion/SubversionNodeExtension.cs \
	MonoDevelop.VersionControl.Subversion/SubversionRepository.cs \
	MonoDevelop.VersionControl.Subversion/SubversionVersionControl.cs \
	MonoDevelop.VersionControl.Subversion/SvnClient.cs

ASM_CONFIG = MonoDevelop.VersionControl.Subversion.dll.config

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.VersionControl.Subversion.addin.xml

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
