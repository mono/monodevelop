TARBALL_PATH=../../tarballs/external

all:

clean:

if ENABLE_MACPLATFORM
all:
	$(MAKE) -C monomac/src

clean:
	$(MAKE) -C monomac/src clean
endif

install:

uninstall:

distdir: dist

dist:
	mkdir -p $(TARBALL_PATH)
	@for dir in `ls .`; do \
		if test -d $$dir; then \
			echo Creating tarball for $$dir; \
			cd $$dir && git archive --prefix=monodevelop-@PACKAGE_VERSION@/external/$$dir/ HEAD | bzip2 > ../$(TARBALL_PATH)/$$dir-@PACKAGE_VERSION@.tar.bz2 && cd ..; \
		fi \
	done	

dist-clean:
	@for dir in `ls .`; do \
		if test -d $$dir; then \
			rm -f $(TARBALL_PATH)/$$dir.tar.bz2; \
		fi \
	done

