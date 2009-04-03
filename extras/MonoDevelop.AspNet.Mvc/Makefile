include config.make
installdir = "$(prefix)/lib/monodevelop/AddIns/MonoDevelop.AspNet.Mvc"
conf=Debug


all:
	PKG_CONFIG_PATH="../../local-config:$(PKG_CONFIG_PATH)" mdtool build -c:$(conf)

clean:
	mdtool build -t:Clean -c:$(conf)

install: all
	mkdir -p  $(installdir)
	cp -r ./build/* $(installdir)

uninstall:
	rm -rf "$(installdir)"
