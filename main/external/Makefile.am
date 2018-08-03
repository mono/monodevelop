TARBALL_PATH=../../tarballs/external
XAMMAC_PATH=/Library/Frameworks/Xamarin.Mac.framework/Versions/Current
XAMMAC_LIB_PATH=$(XAMMAC_PATH)/lib
XAMMAC_PDB=$(XAMMAC_LIB_PATH)/@MAC_ARCHITECTURE@/full/Xamarin.Mac.pdb

if ENABLE_MACPLATFORM
all: Xamarin.Mac.dll Xamarin.Mac.registrar.full.a libxammac.dylib Xamarin.Mac.buildinfo

Xamarin.Mac.dll: $(XAMMAC_LIB_PATH)/@MAC_ARCHITECTURE@/full/Xamarin.Mac.dll
	cp $< $@
	if [[ -f $<.mdb ]]; then cp $<.mdb $@.mdb; fi;
	if [[ -f $(XAMMAC_PDB) ]]; then cp $(XAMMAC_PDB) Xamarin.Mac.pdb; fi;

Xamarin.Mac.registrar.full.a: $(XAMMAC_LIB_PATH)/mmp/Xamarin.Mac.registrar.full.a
	cp $< $@

libxammac.dylib: $(XAMMAC_LIB_PATH)/libxammac.dylib
	cp $< $@

Xamarin.Mac.buildinfo: $(XAMMAC_PATH)/buildinfo
	cp $< $@

clean:
	rm -f Xamarin.Mac.Registrar.full.a
	rm -f Xamarin.Mac.dll*
	rm -f Xamarin.Mac.pdb
	rm -f libxammac.dylib

else
all:

clean:

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

