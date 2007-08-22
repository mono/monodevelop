clean-local:
	-rm -f $(CLEANFILES)

install-local:
uninstall-local:

distlocal:
	list='$(EXTRA_DIST)'; \
	for f in Makefile $$list; do \
		d=`dirname "$$f"`; \
		test -d "$(distdir)/$$d" || \
			mkdir -p "$(distdir)/$$d"; \
		cp -p "$$f" "$(distdir)/$$d" || exit 1; \
	done

distlocal-recursive:
	for dir in $(SUBDIRS); do \
		mkdir -p $(distdir)/$$dir || true; \
		case $$dir in \
		.) make distlocal distdir=$(distdir) || exit 1;; \
		*) (cd $$dir; make distlocal distdir=$(distdir)/$$dir) || exit 1; \
		esac \
	done
