clean-local:
	-rm -f $(CLEANFILES)

distlocal:
	list='$(EXTRA_DIST)'; \
	for f in Makefile $$list; do \
		d=`dirname "$$f"`; \
		test -d "$(distdir)/$$d" || \
			mkdir -p "$(distdir)/$$d"; \
			cp -p "$$f" "$(distdir)/$$d"; \
	done
