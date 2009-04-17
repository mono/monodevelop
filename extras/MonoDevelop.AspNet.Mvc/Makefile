include config.make
installdir = "$(prefix)/lib/monodevelop/AddIns/MonoDevelop.AspNet.Mvc"
conf=Debug


ISLOCAL := $(wildcard "../../local-config/monodevelop.pc")
ifeq ($(strip $(ISLOCAL)),)
	LOCAL_MDBUILD=../../main/build
	MDTOOL=\
		PKG_CONFIG_PATH="../../local-config:${PKG_CONFIG_PATH}" \
		MONODEVELOP_LOCALE_PATH="${LOCAL_MDBUILD}/locale" \
		MONO_ADDINS_REGISTRY="${LOCAL_MDBUILD}/bin" \
		mono --debug "${LOCAL_MDBUILD}/bin/mdrun.exe"
else
	MDTOOL=mdtool
endif

all:
	$(MDTOOL) build -c:$(conf)

clean:
	$(MDTOOL) build -t:Clean -c:$(conf)

install: all
	mkdir -p  $(installdir)
	cp -r ./build/* $(installdir)

uninstall:
	rm -rf "$(installdir)"
