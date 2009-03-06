BUNDLE_VERSION=0.0.5
MAC_APP_DIR=MonoDevelop.app
MACOS=$(MAC_APP_DIR)/Contents/MacOS
MAC_APP_LIB_DIR=$(MACOS)/lib/monodevelop
ADDINS=$(MACOS)/lib/monodevelop/AddIns

CLEANFILES = MonoDevelop.dmg MonoDevelop.app
#DISTCLEANFILES = 
noinst_DATA = MonoDevelop.app.zip
EXTRA_DIST = COPYING dmg-bg.png DS_Store Info.plist.in INSTALL make-dmg-bundle.sh mdtool monodevelop 

all: MonoDevelop.app.zip

#MonoDevelop.dmg: MonoDevelop.app
#	./make-dmg-bundle.sh

clean-local:
	rm -rf MonoDevelop.app
	rm -rf MonoDevelop.dmg
	rm -f MonoDevelop.app.zip

MonoDevelop.app: ../AddIns/* ../AddIns/*/* ../AddIns/*/*/* ../bin/* ../data/*/* ../locale/*/*/*
	mkdir -p $(MAC_APP_DIR)/Contents/{MacOS,Resources}
	mkdir -p $(MAC_APP_DIR)/Contents/MacOS/{lib,share}
	mkdir -p $(MAC_APP_LIB_DIR)
	cp -pR ../AddIns $(MAC_APP_LIB_DIR)
	cp -pR ../data $(MAC_APP_LIB_DIR)
	cp -pR ../bin $(MAC_APP_LIB_DIR)
	cp -pR ../locale $(MAC_APP_DIR)/Contents/MacOS/share
	
	sed -e "s/@BUNDLE_VERSION@/$(BUNDLE_VERSION)/" -e "s/@ASSEMBLY_VERSION@/$(ASSEMBLY_VERSION)/" Info.plist.in > $(MAC_APP_DIR)/Contents/Info.plist
	cp ../../theme-icons/Mac/monodevelop.icns $(MAC_APP_DIR)/Contents/Resources
	cp monodevelop $(MACOS)/monodevelop
	cp mdtool $(MACOS)/mdtool
	
# copy monodevelop.pc and mono-addins.pc to Contents/MacOS/lib/pkgconfig
	mkdir -p $(MACOS)/lib/pkgconfig
	cp ../../*.pc $(MACOS)/lib/pkgconfig

# Copy main/monodevelop.xml to MonoDevelop.app/Contents/MacOS/share/mime/packages
	mkdir -p $(MACOS)/share/{applications,mime,icons,locale,man}
	mkdir -p $(MACOS)/share/mime/packages
	cp ../../monodevelop.xml $(MACOS)/share/mime/packages
	cp ../../monodevelop.desktop $(MACOS)/share/applications

# Copy icons from main/theme-icons/Mac/png to .app/Contents/MacOS/share/icons
	mkdir -p $(MACOS)/share/icons
	cp ../../theme-icons/Mac/png/* $(MACOS)/share/icons

# Remove addins that don't work on OSX
	rm -rf  $(ADDINS)/GnomePlatform* \
	  $(ADDINS)/WindowsPlatform* \
	  $(ADDINS)/MonoDevelop.GtkCore* \
	  $(ADDINS)/MonoDevelop.Moonlight* \
	  $(ADDINS)/VersionControl/MonoDevelop.VersionControl.Subversion* \
	  $(ADDINS)/MonoDevelop.Gettext/*
	
# touch the directory so that update dependencies work
	touch MonoDevelop.app	  


MonoDevelop.app.zip: MonoDevelop.app
	zip -r9uq MonoDevelop.app.zip MonoDevelop.app

