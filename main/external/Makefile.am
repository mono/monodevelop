TARBALL_PATH=../../tarballs/external

all:

clean:

if ENABLE_MACPLATFORM
all: Xamarin.Mac.dll
	$(MAKE) -C monomac/src

Xamarin.Mac.dll: /Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/@MAC_ARCHITECTURE@/full/Xamarin.Mac.dll
	cp -p $< $@
	cp -p $<.mdb $@.mdb
	cp -p /Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mmp/Xamarin.Mac.registrar.full.a .
	cp -p /Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/libxammac.dylib .

clean:
	$(MAKE) -C monomac/src clean
	rm -f Xamarin.Mac.dll*
	rm -f libxammac.dylib
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

.PHONY: Xamarin.Mac.dll 
