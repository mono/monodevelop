
BUNDLE_VERSION=0.0.3
MAC_APP_DIR=MonoDevelop.app
MACOS=$(MAC_APP_DIR)/Contents/MacOS
MAC_APP_LIB_DIR=$(MACOS)/lib/monodevelop
ADDINS=$(MACOS)/lib/monodevelop/AddIns

CLEANFILES = MonoDevelop.dmg
#DISTCLEANFILES = 
noinst_DATA = MonoDevelop.dmg
EXTRA_DIST = COPYING dmg-bg.png DS_Store Info.plist.in INSTALL make-dmg-bundle.sh mdtool monodevelop 

MonoDevelop.dmg: MonoDevelop.app
	./make-dmg-bundle.sh
	#mkdir -p tmp/.background
	#cp -pR MonoDevelop.app tmp
	#cp DS_Store tmp/.DS_Store
	#cp INSTALL COPYING tmp
	#cp dmg-bg.png tmp/.background
	#ln -s /Applications tmp
	
	#hdiutil create MonoDevelop.dmg -volname "MonoDevelop" -fs HFS+ -srcfolder tmp

	#rm -rf tmp

clean-local:
	rm -rf MonoDevelop.app

MonoDevelop.app: 
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


# Remove Gettext plugin (It's broken on OSX)
	rm -rf $(ADDINS)/*Gettext*

