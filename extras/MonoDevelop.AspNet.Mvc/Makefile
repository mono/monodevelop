include config.make
installdir = "$(prefix)/lib/monodevelop/AddIns/MonoDevelop.AspNet.Mvc"
conf=Debug

all:
	mdtool build -c:$(conf)

clean:
	mdtool build -t:Clean -c:$(conf)

install: all
	mkdir -p  $(installdir)
	cp -r "build/*" $(installdir)

uninstall:
	rm -rf "$(installdir)"
