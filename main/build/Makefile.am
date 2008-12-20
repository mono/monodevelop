
if ENABLE_MACPLATFORM
BUNDLE_VERSION=0.0.3
MAC_APP_DIR=MonoDevelop.app
MACOS=$(MAC_APP_DIR)/Contents/MacOS
MAC_APP_LIB_DIR=$(MACOS)/lib/monodevelop

all: app-dir

else
all:

endif

clean-local:
	rm -rf bin
	rm -rf AddIns
	rm -rf MonoDevelop.app


app-dir:
	mkdir -p $(MAC_APP_DIR)/Contents/{MacOS,Resources}
	mkdir -p $(MAC_APP_LIB_DIR)
	cp -a AddIns $(MAC_APP_LIB_DIR)
	cp -a data $(MAC_APP_LIB_DIR)
	cp -a bin $(MAC_APP_LIB_DIR)

	
	sed -e "s/@BUNDLE_VERSION@/$(BUNDLE_VERSION)/" -e "s/@ASSEMBLY_VERSION@/$(ASSEMBLY_VERSION)/" MacOSX/Info.plist.in > $(MAC_APP_DIR)/Contents/Info.plist
	cp ../theme-icons/Mac/monodevelop.icns $(MAC_APP_DIR)/Contents/Resources
	cp MacOSX/monodevelop.in $(MAC_APP_DIR)/Contents/MacOS/monodevelop
	cp MacOSX/mdtool.in $(MAC_APP_DIR)/Contents/MacOS/mdtool


