include $(top_srcdir)/xbuild.include

SUBDIRS = external . contrib src po man theme-icons build tests

MD_CONFIGURE=$(top_srcdir)/../scripts/configure.sh

#capture aclocal flags for autoreconf
ACLOCAL_AMFLAGS=$(ACLOCAL_FLAGS)

bin_SCRIPTS = monodevelop mdtool

buildinfo = $(top_builddir)/build/bin/buildinfo

all: vcrevision

all-local: vcrevision restore-packages sln_build

clean: clean-local

clean-local: sln_clean
	cd external && $(MAKE) clean
	cd build && $(MAKE) clean

NUGET_FOUND = $$(echo $$(which nuget))
NUGET_RESTORE = mono external/nuget-binary/nuget.exe restore -DisableParallelProcessing;

#FIXME: move the restore logic into MSBuild (Before.sln.targets),
#       see: https://github.com/kzu/NuGet.Restore
restore-packages:
	@$(NUGET_RESTORE)
	msbuild /t:Restore /p:RestoreDisableParallel=true external/RefactoringEssentials/RefactoringEssentials/RefactoringEssentials.csproj
	mono external/fsharpbinding/.paket/paket.bootstrapper.exe
	pushd . && cd external/fsharpbinding && mono .paket/paket.exe restore && popd

vcrevision:
	touch vcrevision

$(buildinfo):
	@mkdir -p "$(top_builddir)/build/bin"
# in a tarball, there is a generated buildinfo, which needs to be copied to the right place
# otherwise, the file buildinfo will be generated based on git revision etc.
	if test -f "$(top_srcdir)/buildinfo"; then cp "$(top_srcdir)/buildinfo" "$(top_builddir)/build/bin/buildinfo"; else $(MD_CONFIGURE) gen-buildinfo "$(top_builddir)/build/bin"; fi
	@echo "Updated build information"
	@cat $(buildinfo)

buildinfo: $(buildinfo)

buildinfodir = $(MD_ASSEMBLY_DIR)
buildinfo_DATA = $(buildinfo)

desktopdir = $(datadir)/applications
desktop_DATA = monodevelop.desktop

appdatadir = $(datadir)/appdata
appdata_DATA = monodevelop.appdata.xml

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
	external/fsharpbinding/MonoDevelop.FSharpBinding/FSharpBinding.addin.xml \
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

MD_LAUNCH=$(MD_LAUNCH_SETUP) $(MD_LAUNCH_EXTRA_ENV) UBUNTU_MENUPROXY=0 LIBOVERLAY_SCROLLBAR=0 exec -a "monodevelop" $(RUNTIME)

if ENABLE_MACPLATFORM
run: run-bundle
else
run: run-sgen
endif

run-bundle: $(PROGRAM)
	LANG= LC_ALL= $(MD_LAUNCH_SETUP) $(MD_LAUNCH_EXTRA_ENV) $(MD_BIN_PATH)/MonoDevelop --no-redirect

run-leaks: $(PROGRAM)
	@if [ -a `pwd`/build/bin/libgobject-tracker.dylib ]; then \
		DYLD_FORCE_FLAT_NAMESPACE=1 DYLD_INSERT_LIBRARIES=`pwd`/build/bin/libgobject-tracker.dylib LANG= LC_ALL= $(MD_LAUNCH_SETUP) $(MD_LAUNCH_EXTRA_ENV) $(MD_BIN_PATH)/MonoDevelop --no-redirect; \
	else \
		echo '\033[0;31m' \
		&& echo "You need to compile and install libgobject-tracker.dylib from https://github.com/alanmcgovern/gobject-tracker to run gobject leak checking" \
		&& echo '\033[0m'; \
	fi

run-no-accessibility: $(PROGRAM)
	DISABLE_ATKCOCOA=yes LANG= LC_ALL= $(MD_LAUNCH_SETUP) $(MD_LAUNCH_EXTRA_ENV) $(MD_BIN_PATH)/MonoDevelop --no-redirect

run-64: run-sgen-64

run-boehm: runmd

run-sgen: runmd-sgen

runmd: $(PROGRAM)
	$(MD_LAUNCH)$(SGEN_SUFFIX) --debug $(MD_BIN_PATH)/MonoDevelop.exe --no-redirect

runmd-sgen: $(PROGRAM)
	$(MD_LAUNCH)-sgen$(SGEN_SUFFIX) --debug $(MD_BIN_PATH)/MonoDevelop.exe --no-redirect

run-debug: $(PROGRAM)
	MONODEVELOP_CONSOLE_LOG_LEVEL=All $(MD_LAUNCH)$(SGEN_SUFFIX) --debug=casts $(MD_BIN_PATH)/MonoDevelop.exe --no-redirect

run-gdb: $(PROGRAM)
	echo "run --debug $(MD_BIN_PATH)/MonoDevelop.exe" > gdb.commands
	$(MD_LAUNCH_SETUP) gdb $(RUNTIME)$(SGEN_SUFFIX) -x gdb.commands
	rm -f gdb.commands

run-gdb-sgen: $(PROGRAM)
	echo "run --debug $(MD_BIN_PATH)/MonoDevelop.exe" > gdb.commands
	$(MD_LAUNCH_SETUP) gdb $(RUNTIME)-sgen$(SGEN_SUFFIX) -x gdb.commands
	rm -f gdb.commands

run-profile: $(PROGRAM)
	$(MD_LAUNCH)$(SGEN_SUFFIX) --profile=log:calls $(MD_BIN_PATH)/MonoDevelop.exe --no-redirect

run-profile-stat: $(PROGRAM)
	$(MD_LAUNCH)$(SGEN_SUFFIX) --profile=log:sample $(MD_BIN_PATH)/MonoDevelop.exe --no-redirect

run-trace: $(PROGRAM)
	$(MD_LAUNCH_SETUP) MONODEVELOP_TRACE=1 exec strace -ttt -f -o md.strace mono$(SGEN_SUFFIX) $(MD_BIN_PATH)/MonoDevelop.exe --no-redirect

plot-trace:
	if [ ! -e 'plot-timeline.py' ]; then wget http://gitorious.org/performance-scripts/mainline/blobs/raw/master/plot-timeline.py; fi
	python plot-timeline.py -o md-trace.png  md.strace

run-profile-heap: $(PROGRAM)
	$(MD_LAUNCH)$(SGEN_SUFFIX) --profile=log:heapshot=900000ms $(MD_BIN_PATH)/MonoDevelop.exe --no-redirect

check-addins:
	$(MDTOOL_RUN) setup reg-build -v

test:
	cd tests && $(MAKE) test assembly=$(assembly)

uitest:
	cd tests && $(MAKE) uitest assembly=$(assembly) categories=$(categories) tests=$(tests)

coverage:
	cd tests && $(MAKE) coverage

app-dir: all
	cd build && make app-dir
#mkdir -p $(MAC_APP_DIR)/Contents/{MacOS,Resources}
#mkdir -p $(MAC_APP_LIB_DIR)
#cp -a build/AddIns $(MAC_APP_LIB_DIR)
#cp -a build/data $(MAC_APP_LIB_DIR)
#cp -a build/bin $(MAC_APP_LIB_DIR)

# we can't use the variables that are conditionally defined
EXTRA_DIST = $(bin_SCRIPTS) $(desktop_DATA) $(appdata_DATA) $(pixmap_DATA) \
	monodevelop.xml $(pkgconfig_in_files) external/Makefile.in \
	Main.sln \
	md.targets

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

.PHONY: vcrevision buildinfo
