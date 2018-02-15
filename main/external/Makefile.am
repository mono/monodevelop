TARBALL_PATH=../../tarballs/external
XAMMAC_PATH=/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib
all:

clean:

if ENABLE_MACPLATFORM
all: Xamarin.Mac.dll Xamarin.Mac.registrar.full.a libxammac-system.a libxammac.dylib
	$(MAKE) -C monomac/src

Xamarin.Mac.dll: $(XAMMAC_PATH)/@MAC_ARCHITECTURE@/full/Xamarin.Mac.dll
	cp -p $< $@
	cp -p $<.mdb $@.mdb

Xamarin.Mac.registrar.full.a: $(XAMMAC_PATH)/mmp/Xamarin.Mac.registrar.full.a
	cp -p $< $@

libxammac-system.a: $(XAMMAC_PATH)/libxammac-system.a
	cp -p $< $@

libxammac.dylib: $(XAMMAC_PATH)/libxammac.dylib
	cp -p $< $@

clean:
	$(MAKE) -C monomac/src clean
	rm -f Xamarin.Mac.Registrar.full.a
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
