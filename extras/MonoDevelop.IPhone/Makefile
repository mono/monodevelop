include config.make
installdir = "$(prefix)/lib/monodevelop/AddIns/MonoDevelop.IPhone"
conf=Debug


ISLOCAL := $(wildcard "../local-config/monodevelop.pc")
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

all: update-reg
	$(MDTOOL) build -c:$(conf)

clean:
	rm -rf build/*

# this breaks if the local MD has been cleaned
#clean: update-reg	
#	$(MDTOOL) build -t:Clean -c:$(conf)

install: all
	mkdir -p  $(installdir)
	cp -r ./build/* $(installdir)

uninstall:
	rm -rf "$(installdir)"

ifeq ($(strip $(ISLOCAL)),)
update-reg:
	$(MDTOOL) setup reg-update
else
	update-reg:
endif
