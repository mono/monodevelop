SUBDIRS = contrib src po man theme-icons build tests

#capture aclocal flags for autoreconf
ACLOCAL_AMFLAGS=$(ACLOCAL_FLAGS)

bin_SCRIPTS = monodevelop mdtool

buildinfo = $(top_builddir)/build/bin/buildinfo

all: vcrevision

vcrevision:
	@if test -d ../.git; then \
		version=$$(cd ..; \
			LANG=C; export LANG; \
			git describe --dirty --always --abbrev=0 --match nothing; \
		); \
		echo "$$version" > "vcrevision"; \
	else \
		if test ! -f ./vcrevision; then \
			echo "unknown" > "vcrevision"; \
		fi; \
	fi

$(buildinfo): vcrevision
	@mkdir -p "$(top_builddir)/build/bin"
	@echo "Git revision: $$(cat $(top_srcdir)/vcrevision)" > "$@"
	@echo "Build date: $$(date -u +'%Y-%m-%d %H:%M:%S%z')" >> "$@"
	@echo "Updated build information"
	@cat $(buildinfo)

buildinfodir = $(MD_ASSEMBLY_DIR)
buildinfo_DATA = $(buildinfo)

desktopdir = $(datadir)/applications
desktop_DATA = monodevelop.desktop

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
	$(MDTOOL_RUN) setup p $(PACKAGES:%=%) -d:packages
	$(MDTOOL_RUN) setup rb packages

apidocs:
	monodocer $(DOCFILES:%=--assembly:%) -delete --path docs/api
	mkdir -p docs/sources
	mdassembler --out docs/sources/monodevelop-reference --ecma docs/api
	mautil -reg build/bin reg-build
	mautil -reg build/bin -p build/bin info --all --xml > docs/extension-model.xml
	mdassembler --out docs/sources/monodevelop-extension-guide --addins docs/extension-model.xml
#	cd docs/apiguide && mdassembler --out monodevelop-api-guide --xhtml toc.xhtml && mv monodevelop-api-guide.zip ../web/sources

MD_LAUNCH=$(MD_LAUNCH_SETUP) UBUNTU_MENUPROXY=0 exec -a "monodevelop" $(RUNTIME)

run: runmd

runmd: $(PROGRAM)
	$(MD_LAUNCH) --debug $(MD_BIN_PATH)/MonoDevelop.exe --redirect-output

run-debug: $(PROGRAM)
	MONODEVELOP_CONSOLE_LOG_LEVEL=All $(MD_LAUNCH) --debug=casts $(MD_BIN_PATH)/MonoDevelop.exe

run-profile: $(PROGRAM)
	$(MD_LAUNCH) -O=inline --profile $(MD_BIN_PATH)/MonoDevelop.exe

run-profile-stat: $(PROGRAM)
	$(MD_LAUNCH) --profile=logging:stat $(MD_BIN_PATH)/MonoDevelop.exe

run-trace: $(PROGRAM)
	$(MD_LAUNCH_SETUP) MONODEVELOP_TRACE=1 exec strace -ttt -f -o md.strace mono $(MD_BIN_PATH)/MonoDevelop.exe

plot-trace:
	if [ ! -e 'plot-timeline.py' ]; then wget http://gitorious.org/performance-scripts/mainline/blobs/raw/master/plot-timeline.py; fi
	python plot-timeline.py -o md-trace.png  md.strace

run-profile-heap: $(PROGRAM)
	$(MD_LAUNCH) -O=inline --profile=heap:heaplog $(MD_BIN_PATH)/MonoDevelop.exe
	
check-addins:
	$(MDTOOL_RUN) setup reg-build -v

test:
	cd tests/UnitTests && make test fixture=$(fixture)

app-dir: all
	cd build && make app-dir
#mkdir -p $(MAC_APP_DIR)/Contents/{MacOS,Resources}
#mkdir -p $(MAC_APP_LIB_DIR)
#cp -a build/AddIns $(MAC_APP_LIB_DIR)
#cp -a build/data $(MAC_APP_LIB_DIR)
#cp -a build/bin $(MAC_APP_LIB_DIR)

# we can't use the variables that are conditionally defined
EXTRA_DIST = $(bin_SCRIPTS) $(desktop_DATA) $(pixmap_DATA) \
	monodevelop.xml $(pkgconfig_in_files) vcrevision

DISTCLEANFILES = monodevelop intltool-extract intltool-merge intltool-update \
	$(pkgconfig_DATA) 

include $(top_srcdir)/Makefile.include

#
# Force distuninstallcheck to ignore files created by cache-rebuilding commands (update-mime-database etc)
#
# From BEAST (LGPL license): http://svn.gnome.org/viewvc/beast/trunk/Makefile.am
filter_stale_uninstalled = \
    | egrep -v '^.*/share/mime/.*$$' \
    | egrep -v '^.*./share/icons/hicolor/icon-theme\.cache$$'
    
filter_stale_buildfiles       = | egrep -v '^./report.out$$'
distuninstallcheck_listfiles  = find . -type f -print 		# automake-1.9 setting
distuninstallcheck_listfiles += $(filter_stale_uninstalled)	# amend by required filtering
distcleancheck_listfiles      = find . -type f -print		# automake-1.9 setting
distcleancheck_listfiles     += $(filter_stale_buildfiles)	# amend by required filtering

.PHONY: vcrevision

package-monomac:
	$(MD_LAUNCH_SETUP) mono --debug "build/bin/mdtool.exe" setup pack ../extras/MonoDevelop.MonoMac/build/MonoDevelop.MonoMac.dll
