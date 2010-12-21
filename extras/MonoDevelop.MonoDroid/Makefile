include config.make
installdir = "$(prefix)/lib/monodevelop/AddIns/MonoDevelop.MonoDroid"
conf=Debug
SLN=MonoDevelop.MonoDroid.sln
VERBOSITY=normal
XBUILD_ARGS=/verbosity:$(VERBOSITY) /nologo

srcdir_abs=$(shell pwd)
LOCAL_CONFIG=$(srcdir_abs)/../../local-config

ifeq ($(strip $(wildcard "${LOCAL_CONFIG}/monodevelop.pc")),)
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
