SUBDIRS = contrib src build po man

bin_SCRIPTS = monodevelop mdtool

all:

desktopdir = $(datadir)/applications
desktop_DATA = monodevelop.desktop

pixmapdir = $(datadir)/pixmaps
pixmap_DATA = monodevelop.png

xdgmimedir = $(datadir)/mime/packages
xdgmime_DATA = monodevelop.xml

pkgconfig_in_files = monodevelop.pc.in monodevelop-core-addins.pc.in

pkgconfigdir= $(prefix)/lib/pkgconfig
pkgconfig_DATA = $(pkgconfig_in_files:.pc.in=.pc)

if ENABLE_UPDATE_MIMEDB

install-data-hook:
	$(UPDATE_MIME_DB) $(DESTDIR)$(datadir)/mime

uninstall-hook:
	$(UPDATE_MIME_DB) $(DESTDIR)$(datadir)/mime

endif # ENABLE_UPDATE_MIMEDB

PACKAGES = \
	build/bin/MonoDevelop.Core.Gui.addin.xml \
	build/AddIns/MonoDevelop.Documentation.addin.xml \
	build/bin/MonoDevelop.Ide.addin.xml \
	build/bin/MonoDevelop.Projects.addin.xml \
	build/bin/MonoDevelop.Projects.Gui.addin.xml \
	build/AddIns/MonoDevelop.SourceEditor.addin.xml \
	build/AddIns/BackendBindings/BooBinding.addin.xml \
	build/AddIns/BackendBindings/CSharpBinding.addin.xml \
	build/AddIns/BackendBindings/ILAsmBinding.addin.xml \
	build/AddIns/BackendBindings/JavaBinding.addin.xml \
	build/AddIns/BackendBindings/VBNetBinding.addin.xml \
	build/AddIns/MonoQuery/MonoQuery.addin.xml \
	build/AddIns/NUnit/MonoDevelopNUnit.addin.xml \
	build/AddIns/MonoDeveloperExtensions/MonoDeveloperExtensions.addin.xml \
	build/AddIns/prj2makesharp/prj2make-sharp-lib.addin.xml \
	build/AddIns/MonoDevelop.GtkCore/MonoDevelop.GtkCore.addin.xml \
	build/AddIns/ChangeLogAddIn/ChangeLogAddIn.addin.xml \
	build/AddIns/WelcomePage/WelcomePage.addin.xml \
	build/AddIns/MonoDevelop.Autotools/MonoDevelop.Autotools.addin.xml \
	build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.addin.xml \
	build/AddIns/AspNetAddIn/AspNetAddIn.addin.xml \
	build/AddIns/AspNetAddIn/AspNetEdit.addin.xml

DOCFILES = \
	build/bin/MonoDevelop.Core.dll \
	build/bin/MonoDevelop.Components.dll \
	build/bin/MonoDevelop.Core.Gui.dll \
	build/AddIns/MonoDevelop.Documentation.dll \
	build/bin/MonoDevelop.Ide.dll \
	build/bin/MonoDevelop.Projects.dll \
	build/bin/MonoDevelop.Projects.Gui.dll

mpacks: $(PACKAGES)
	test -z "packages" || mkdir -p packages
	mono build/bin/mdrun.exe setup p $(PACKAGES:%=%) -d:packages
	mono build/bin/mdrun.exe setup rb packages

apidocs:
	monodocer $(DOCFILES:%=--assembly:%) -delete --path docs/api
	mkdir -p docs/sources
	mdassembler --out docs/sources/monodevelop-reference --ecma docs/api
	mautil -reg build/bin reg-build
	mautil -reg build/bin -p build/bin info --all --xml > docs/extension-model.xml
	mdassembler --out docs/sources/monodevelop-extension-guide --addins docs/extension-model.xml
#	cd docs/apiguide && mdassembler --out monodevelop-api-guide --xhtml toc.xhtml && mv monodevelop-api-guide.zip ../web/sources

### SYNCH BLOCK: any changes to this line should be kept in sync with the corresponding block in Makefile.am
MD_PKG_CONFIG_PATH=$(gtksharp_prefix)/lib/pkgconfig/:/usr/lib/pkgconfig/:/usr/local/lib/pkgconfig/:/usr/share/pkgconfig/:/usr/local/share/pkgconfig/$(shell if test -d /usr/lib64; then echo ":/usr/lib64/pkgconfig/:/usr/local/lib64/pkgconfig/"; fi)
### END BLOCK

MD_LAUNCH_SETUP= \
	cd build/bin && \
	MONO_ADDINS_REGISTRY=. MOZILLA_FIVE_HOME=$(MOZILLA_HOME) \
	LD_LIBRARY_PATH=`echo "$(LD_LIBRARY_PATH):$(MOZILLA_HOME)" | sed 's/^://g'` \
	PKG_CONFIG_PATH=`echo "$(PKG_CONFIG_PATH):$(MD_PKG_CONFIG_PATH)" | sed 's/^://g'`

MD_LAUNCH=$(MD_LAUNCH_SETUP) exec -a "monodevelop" $(RUNTIME)

run: runmd

runmd: $(PROGRAM)
	$(MD_LAUNCH) --debug MonoDevelop.exe

run-profile: $(PROGRAM)
	$(MD_LAUNCH) -O=inline --profile MonoDevelop.exe

run-profile-stat: $(PROGRAM)
	$(MD_LAUNCH) --profile=default:stat MonoDevelop.exe

run-profile-heap: $(PROGRAM)
	$(MD_LAUNCH) -O=inline --profile=heap:heaplog MonoDevelop.exe
	
check-addins:
	$(MD_LAUNCH_SETUP) $(RUNTIME) --debug mdrun.exe setup reg-build -v

# we can't use the variables that are conditionally defined
EXTRA_DIST = $(bin_SCRIPTS) $(desktop_DATA) $(pixmap_DATA) \
	monodevelop.xml mkinstalldirs intltool-extract.in \
	intltool-merge.in intltool-update.in $(pkgconfig_in_files)

DISTCLEANFILES = monodevelop intltool-extract intltool-merge intltool-update \
	$(pkgconfig_DATA) 
