include $(top_srcdir)/xbuild.include

if ENABLE_MACPLATFORM
SUBDIRS = external po theme-icons tests build/MacOSX
else
SUBDIRS = external po theme-icons tests
endif

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
	mono external/fsharpbinding/.paket/paket.bootstrapper.exe -v 5.201.1
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

deploy-tests:
	cd tests && $(MAKE) deploy-tests

uitest:
	cd tests && $(MAKE) uitest assembly=$(assembly) categories=$(categories) tests=$(tests)

performance-tests:
	cd tests && $(MAKE) performance-tests

coverage:
	cd tests && $(MAKE) coverage

app-dir: all
	cd build/MacOSX && make app-dir

include $(top_srcdir)/Makefile.include

.PHONY: vcrevision buildinfo
