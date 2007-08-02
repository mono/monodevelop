clean-local:
	-rm -f $(CLEANFILES)

install-local:

distlocal:
	list='$(EXTRA_DIST)'; \
	for f in Makefile $$list; do \
		d=`dirname "$$f"`; \
		test -d "$(distdir)/$$d" || \
			mkdir -p "$(distdir)/$$d"; \
		cp -p "$$f" "$(distdir)/$$d"; \
	done

distlocal-recursive:
	for dir in $(SUBDIRS); do \
		mkdir $(distdir)/$$dir || true; \
		case $$dir in \
		.) make distlocal distdir=$(distdir);; \
		*) (cd $$dir; make distlocal distdir=../$(distdir)/$$dir); \
		esac \
	done
