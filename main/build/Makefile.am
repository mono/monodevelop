
if ENABLE_MACPLATFORM
SUBDIRS = MacOSX
else

SUBDIRS=
endif

all:

install:
	mkdir -p $(DESTDIR)$(prefix)/lib/monodevelop/bin
	cp -rf bin $(DESTDIR)$(prefix)/lib/monodevelop/
	cp -rf AddIns $(DESTDIR)$(prefix)/lib/monodevelop/
	cp -rf locale $(DESTDIR)$(prefix)/lib/monodevelop/
	cp -rf data $(DESTDIR)$(prefix)/lib/monodevelop/

clean-local:
	rm -rf bin AddIns locale data


