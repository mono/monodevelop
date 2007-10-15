clean-local:
	make pre-clean-local-hook
	make $(CONFIG)_BeforeClean
	-rm -f $(CLEANFILES)
	make $(CONFIG)_AfterClean
	make post-clean-local-hook

install-local:
uninstall-local:

dist-local:
	make pre-dist-local-hook distdir=$$distdir
	list='$(EXTRA_DIST)'; \
	for f in Makefile $$list; do \
		d=`dirname "$$f"`; \
		test -d "$(distdir)/$$d" || \
			mkdir -p "$(distdir)/$$d"; \
		cp -p "$$f" "$(distdir)/$$d" || exit 1; \
	done
	make post-dist-local-hook distdir=$$distdir

dist-local-recursive:
	for dir in $(SUBDIRS); do \
		mkdir -p $(distdir)/$$dir || true; \
		case $$dir in \
		.) make dist-local distdir=$(distdir) || exit 1;; \
		*) (cd $$dir; make dist-local distdir=$(distdir)/$$dir) || exit 1; \
		esac \
	done

#hooks: Available hooks - all, clean, install, uninstall and dist
#	and their *-local variants
pre-%-hook: ; @:
post-%-hook: ; @:

#targets for custom commands
%_BeforeBuild: ; @:
%_AfterBuild: ; @:
%_BeforeClean: ; @:
%_AfterClean: ; @:
