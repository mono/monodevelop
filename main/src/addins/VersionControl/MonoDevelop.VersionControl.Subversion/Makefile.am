
ADDIN_BUILD = $(top_builddir)/build/AddIns/VersionControl
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.VersionControl.Subversion.dll

DEPS =  \
	$(top_builddir)/build/AddIns/VersionControl/MonoDevelop.VersionControl.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

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
	MonoDevelop.VersionControl.Subversion/SubversionNodeExtension.cs \
	MonoDevelop.VersionControl.Subversion/SubversionRepository.cs \
	MonoDevelop.VersionControl.Subversion/SubversionVersionControl.cs \
	MonoDevelop.VersionControl.Subversion/SvnRevision.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.VersionControl.Subversion.addin.xml

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -unsafe $(build_sources) $(REFS) $(build_deps) $(build_resources:%=/resource:%) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

if ENABLE_SUBVERSION
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

if ENABLE_SUBVERSION
addindir = $(MD_ADDIN_DIR)/VersionControl
addin_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

include $(top_srcdir)/Makefile.include
