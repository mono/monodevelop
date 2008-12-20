

if ENABLE_MACPLATFORM
BUNDLE_VERSION=0.0.3
MAC_APP_DIR=MonoDevelop.app
MACOS=$(MAC_APP_DIR)/Contents/MacOS
MAC_APP_LIB_DIR=$(MACOS)/lib/monodevelop
ADDINS=$(MACOS)/lib/monodevelop/AddIns

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
	mkdir -p $(MAC_APP_DIR)/Contents/MacOS/{lib,share}
	mkdir -p $(MAC_APP_LIB_DIR)
	cp -a AddIns $(MAC_APP_LIB_DIR)
	cp -a data $(MAC_APP_LIB_DIR)
	cp -a bin $(MAC_APP_LIB_DIR)
	cp -a locale $(MAC_APP_DIR)/Contents/MacOS/share

	sed -e "s/@BUNDLE_VERSION@/$(BUNDLE_VERSION)/" -e "s/@ASSEMBLY_VERSION@/$(ASSEMBLY_VERSION)/" MacOSX/Info.plist.in > $(MAC_APP_DIR)/Contents/Info.plist
	cp ../theme-icons/Mac/monodevelop.icns $(MAC_APP_DIR)/Contents/Resources
	cp MacOSX/monodevelop.in $(MACOS)/monodevelop
	cp MacOSX/mdtool.in $(MACOS)/mdtool

# copy monodevelop.pc and mono-addins.pc to Contents/MacOS/lib/pkgconfig
	mkdir -p $(MACOS)/lib/pkgconfig
	cp ../*.pc $(MACOS)/lib/pkgconfig

# Copy main/monodevelop.xml to MonoDevelop.app/Contents/MacOS/share/mime/packages
	mkdir -p $(MACOS)/share/mime/packages
	cp ../monodevelop.xml $(MACOS)/share/mime/packages

# Copy icons from main/theme-icons/Mac/png to .app/Contents/MacOS/share/icons
	mkdir -p $(MACOS)/share/icons
	cp ../theme-icons/Mac/png/* $(MACOS)/share/icons


# Remove Gettext plugin (It's broken on OSX)
	rm -rf $(ADDINS)/*Gettext*

