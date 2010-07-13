include config.make
installdir = "$(prefix)/lib/monodevelop/AddIns/MonoDevelop.MonoMac"
conf=Debug
SLN=MonoDevelop.MonoMac.sln
VERBOSITY=normal
XBUILD_ARGS=/verbosity:$(VERBOSITY) /nologo

ISLOCAL := $(wildcard "../local-config/monodevelop.pc")
ifeq ($(strip $(ISLOCAL)),)
	LOCAL_CONFIG="`pwd`/../../local-config"
	XBUILD=PKG_CONFIG_PATH="${LOCAL_CONFIG}:${PKG_CONFIG_PATH}" xbuild $(XBUILD_ARGS)
else
	XBUILD=xbuild $(XBUILD_ARGS)
endif

all: 
	$(XBUILD) $(SLN) /property:Configuration=$(conf)

clean:
	$(XBUILD) $(SLN) /property:Configuration=$(conf) /t:Clean
	rm -rf build/*

install: all
	mkdir -p  $(installdir)
	cp -r ./build/* $(installdir)

uninstall:
	rm -rf "$(installdir)"
