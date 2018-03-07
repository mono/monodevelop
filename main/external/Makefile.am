TARBALL_PATH=../../tarballs/external
XAMMAC_PATH=/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib
XAMMAC_PDB=$(XAMMAC_PATH)/@MAC_ARCHITECTURE@/full/Xamarin.Mac.pdb
all:

clean:

if ENABLE_MACPLATFORM
all: Xamarin.Mac.dll Xamarin.Mac.registrar.full.a libxammac.dylib
	$(MAKE) -C monomac/src

Xamarin.Mac.dll: $(XAMMAC_PATH)/@MAC_ARCHITECTURE@/full/Xamarin.Mac.dll
	cp -p $< $@
	if [[ -f $<.mdb ]]; then cp -p $<.mdb $@.mdb; fi;
	if [[ -f $(XAMMAC_PDB) ]]; then cp -p $(XAMMAC_PDB) Xamarin.Mac.pdb; fi;

Xamarin.Mac.registrar.full.a: $(XAMMAC_PATH)/mmp/Xamarin.Mac.registrar.full.a
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
