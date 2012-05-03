
if ENABLE_MACBUNDLE
SUBDIRS = MacOSX
else

SUBDIRS=
endif

all:

install:
	mkdir -p $(DESTDIR)$(prefix)/lib/monodevelop/bin
	cp -rf bin $(DESTDIR)$(prefix)/lib/monodevelop/
	cp -rf AddIns $(DESTDIR)$(prefix)/lib/monodevelop/AddIns
	cp -rf locale $(DESTDIR)$(prefix)/lib/monodevelop/locale
	cp -rf data $(DESTDIR)$(prefix)/lib/monodevelop/data

clean-local:
	rm -rf bin AddIns locale data


