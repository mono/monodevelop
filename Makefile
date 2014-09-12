include main/monodevelop_version

EXTRA_DIST = configure
SPACE := 
SPACE +=  
AOT_DIRECTORIES:=$(subst $(SPACE),:,$(shell find main/build/* -type d))
MONO_AOT:=MONO_PATH=$(AOT_DIRECTORIES):$(MONO_PATH) mono --aot --debug

all: update_submodules all-recursive

update_submodules:
	if test -d ".git"; then \
		git submodule update --init --recursive || exit 1; \
	fi

top_srcdir=.
include $(top_srcdir)/config.make

CONFIG_MAKE=$(top_srcdir)/config.make

%-recursive: $(CONFIG_MAKE)
	@export PKG_CONFIG_PATH="`pwd`/$(top_srcdir)/local-config:$(prefix)/lib/pkgconfig:$(prefix)/share/pkgconfig:$$PKG_CONFIG_PATH"; \
	export MONO_GAC_PREFIX="$(prefix):$$MONO_GAC_PREFIX"; \
	set . $$MAKEFLAGS; final_exit=:; \
	case $$2 in --unix) shift ;; esac; \
	case $$2 in *=*) dk="exit 1" ;; *k*) dk=: ;; *) dk="exit 1" ;; esac; \
	for dir in $(SUBDIRS); do \
		case $$dir in \
		.) $(MAKE) $*-local || { final_exit="exit 1"; $$dk; };;\
		*) (cd $$dir && $(MAKE) $*) || { final_exit="exit 1"; $$dk; };;\
		esac \
	done
	$$final_exit

$(CONFIG_MAKE): $(top_srcdir)/configure
	@if test -e "$(CONFIG_MAKE)"; then exec $(top_srcdir)/configure --prefix=$(prefix); \
	else \
		echo "You must run configure first"; \
		exit 1; \
	fi

clean: clean-recursive
install: install-recursive
uninstall: uninstall-recursive
distcheck: distcheck-recursive

distclean: distclean-recursive
	rm -rf config.make local-config

remove-stale-tarballs:
	rm -rf tarballs

dist: update_submodules remove-stale-tarballs dist-recursive
	mkdir -p tarballs
	for t in $(SUBDIRS); do \
		if test -e $$t/*.tar.gz; then \
			mv -f $$t/*.tar.gz tarballs ;\
		fi \
	done
	for t in `ls tarballs/*.tar.gz`; do \
		gunzip $$t ;\
	done
	for t in `ls tarballs/*.tar`; do \
		bzip2 $$t ;\
	done
	rm -rf specs
	mkdir -p specs
	for t in $(SUBDIRS); do \
		if test -a $$t/*.spec; then \
			cp -f $$t/*.spec specs ;\
		fi \
	done
	@cd tarballs && for tb in `ls external`; do \
		echo Decompressing $$tb; \
		tar xvjf external/$$tb; \
	done
	@rm -rf tarballs/external	
	@echo Decompressing monodevelop-$(PACKAGE_VERSION).tar.bz2
	@cd tarballs && tar xvjf monodevelop-$(PACKAGE_VERSION).tar.bz2
	@mozroots --import --machine --sync
	@yes | certmgr -ssl -m https://go.microsoft.com
	@yes | certmgr -ssl -m https://nugetgallery.blob.core.windows.net
	@yes | certmgr -ssl -m https://nuget.org
	@cd main/src/addins/AspNet && mono ../../../external/nuget-binary/NuGet.exe restore -SolutionDirectory ../../.. -verbosity detailed
	@cp -R main/packages tarballs/monodevelop-$(PACKAGE_VERSION)
	@cp version.config tarballs/monodevelop-$(PACKAGE_VERSION)
	@cd main && make buildinfo
	@cp main/build/bin/buildinfo tarballs/monodevelop-$(PACKAGE_VERSION)/
	@echo Generating merged tarball
	@cd tarballs && tar -cjf monodevelop-$(PACKAGE_VERSION).tar.bz2 monodevelop-$(PACKAGE_VERSION)
	@cd tarballs && rm -rf monodevelop-$(PACKAGE_VERSION)

aot:
	@for i in main/build/bin/*.dll; do ($(MONO_AOT) $$i &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done
	@for i in main/build/AddIns/*.dll; do ($(MONO_AOT) $$i &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done
	@for i in main/build/AddIns/*/*.dll; do ($(MONO_AOT) $$i &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done
	@for i in main/build/AddIns/*/*/*.dll; do ($(MONO_AOT) $$i &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done

run:
	cd main && $(MAKE) run

run-boehm:
	cd main && $(MAKE) run-boehm

run-sgen:
	cd main && $(MAKE) run-sgen

run-gdb:
	cd main && $(MAKE) run-gdb

test:
	cd main && $(MAKE) test assembly=$(assembly)

check-addins:
	cd main && $(MAKE) check-addins

app-dir:
	cd main && $(MAKE) app-dir

reset-versions: reset-all
check-versions: check-all

reset-%:
	@./version-checks --reset $*

check-%:
	@./version-checks --check $*
