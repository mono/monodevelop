all:

install:
	cp -rf MonoDevelop.Database $(DESTDIR)$(prefix)/lib/monodevelop/AddIns/
	cp -rf locale $(DESTDIR)$(prefix)/lib/monodevelop/AddIns/MonoDevelop.Database/

clean-local:
	rm -rf MonoDevelop.Database locale
	rm -f *.dll
	rm -f *.dll.mdb
