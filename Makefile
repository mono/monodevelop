
EXTRA_DIST = configure

all: all-recursive

top_srcdir=.
include $(top_srcdir)/config.make

CONFIG_MAKE=$(top_srcdir)/config.make

%-recursive: $(CONFIG_MAKE)
	@set . $$MAKEFLAGS; final_exit=:; \
	case $$2 in --unix) shift ;; esac; \
	case $$2 in *=*) dk="exit 1" ;; *k*) dk=: ;; *) dk="exit 1" ;; esac; \
	for dir in $(SUBDIRS); do \
		case $$dir in \
		.) make $*-local || { final_exit="exit 1"; $$dk; };;\
		*) (cd $$dir && make $*) || { final_exit="exit 1"; $$dk; };;\
		esac \
	done
	$$final_exit

$(CONFIG_MAKE): $(top_srcdir)/configure
	@if test -e "$(CONFIG_MAKE)"; then exec $(top_srcdir)/configure; \
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
	

dist: dist-recursive
	mkdir -p tarballs
	for t in $(SUBDIRS); do \
		if test -a $$t/*.tar.gz; then \
			mv -f $$t/*.tar.gz tarballs ;\
		fi \
	done

run:
	cd main && make run
	
test:
	cd main/tests/UnitTests && make test fixture=$(fixture)

check-addins:
	cd main && make check-addins

app-dir:
	cd main && make app-dir
