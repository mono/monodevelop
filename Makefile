include main/monodevelop_version

EXTRA_DIST = configure code_of_conduct.md
SPACE := 
SPACE +=  

ifeq ($(origin APP), undefined)
BIN_DIR=main/build
else
BIN_DIR="$(APP)/Contents/Resources/lib/monodevelop"
endif

AOT_DIRECTORIES:=$(subst $(SPACE),:,$(shell find $(BIN_DIR)/* -not -path "*azure-functions-cli*" -not -path "*.dSYM*" -not -path "*/ServiceHub/*" -not -path $(BIN_DIR)/tests* -type d))

MONO_DIR=/Library/Frameworks/Mono.framework/Libraries/mono
MSBUILD_PATH=$(MONO_DIR)/msbuild/15.0/bin

# --assembly-loader=strict will not work for valuetuples here, even with binding redirects
AOT_COMMAND=mono64 --aot=hybrid --debug --assembly-loader=strict
MONO_AOT:=MONO_PATH="$(AOT_DIRECTORIES):$(MSBUILD_PATH):$(MONO_PATH)" $(AOT_COMMAND) --apply-bindings=$(BIN_DIR)/bin/MonoDevelop.exe.config

MSBUILD_LIBRARIES=Microsoft.Build.dll Microsoft.Build.Framework.dll Microsoft.Build.Utilities.Core.dll
MSBUILD_DLLS=$(patsubst %, $(MSBUILD_PATH)/%, $(MSBUILD_LIBRARIES))

all: update_submodules all-recursive

GIT_FOUND = $$(echo $$(which git))
SYNC_SUBMODULES = \
	if test -d ".git"; then \
		if [ "x$(GIT_FOUND)" = "x" ]; then \
			echo "git not found; please install it first"; \
			exit 1; \
		fi; \
		git submodule sync; \
		git submodule update --init --recursive || exit 1; \
	fi

update_submodules:
	@$(SYNC_SUBMODULES)

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
		.) PATH="$(PATH):/Library/Frameworks/Mono.framework/Versions/Current/bin" $(MAKE) $*-local || { final_exit="exit 1"; $$dk; };;\
		*) (cd $$dir && PATH="$(PATH):/Library/Frameworks/Mono.framework/Versions/Current/bin" $(MAKE) $*) || { final_exit="exit 1"; $$dk; };;\
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
	@cp version.config tarballs/monodevelop-$(PACKAGE_VERSION)
	@cp NuGet.config tarballs/monodevelop-$(PACKAGE_VERSION)
	@rm -f main/build/bin/buildinfo
	@cd main && make buildinfo
	@cp main/build/bin/buildinfo tarballs/monodevelop-$(PACKAGE_VERSION)/
	@echo Generating merged tarball
	@cd tarballs && tar -cjf monodevelop-$(PACKAGE_VERSION).tar.bz2 monodevelop-$(PACKAGE_VERSION)
	@cd tarballs && rm -rf monodevelop-$(PACKAGE_VERSION)

aot-all: aot aot-gac aot-msbuild-with-copy

aot-msbuild-with-copy: aot-msbuild
	@for i in $(MSBUILD_DLLS); do cp -r "$$i" "$$i.dylib" "$$i.dylib.dSYM" $(BIN_DIR)/bin; done

aot:
	@for i in $(BIN_DIR)/bin/*.dll; do ($(MONO_AOT) "$$i" &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done
	@for i in $(BIN_DIR)/AddIns/*.dll; do ($(MONO_AOT) "$$i" &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done
	@for i in $(BIN_DIR)/AddIns/*/*.dll; do ($(MONO_AOT) "$$i" &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done
	@for i in $(BIN_DIR)/AddIns/*/*/*.dll; do ($(MONO_AOT) "$$i" &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done

aot-gac:
	@for i in $(MONO_DIR)/4.5/*.dll; do (sudo $(AOT_COMMAND) "$$i" &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done
	@for i in $(MONO_DIR)/gac/*/*/*.dll; do (sudo $(AOT_COMMAND) "$$i" &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done

aot-msbuild:
	@for i in $(MSBUILD_DLLS); do (sudo $(AOT_COMMAND) "$$i" &> /dev/null && echo AOT successful: $$i) || (echo AOT failed: $$i); done

run:
	cd main && $(MAKE) run

run-64:
	cd main && $(MAKE) run-64

run-boehm:
	cd main && $(MAKE) run-boehm

run-sgen:
	cd main && $(MAKE) run-sgen

run-gdb:
	cd main && $(MAKE) run-gdb

run-gdb-64:
	cd main && $(MAKE) run-gdb-64

run-leaks:
	cd main && $(MAKE) run-leaks

run-no-accessibility:
	cd main && $(MAKE) run-no-accessibility
test:
	cd main && $(MAKE) test assembly=$(assembly)

uitest:
	cd main && $(MAKE) uitest assembly=$(assembly) tests=$(tests)

coverage:
	cd main && $(MAKE) coverage

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
