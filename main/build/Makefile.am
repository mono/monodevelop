
all:

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

	
	sed -e "s/@BUNDLE_VERSION@/$(BUNDLE_VERSION)/" MacOSX/Info.plist.in > $(MAC_APP_DIR)/Contents/Info.plist
	cp MacOSX/monodevelop.icns $(MAC_APP_DIR)/Contents/Resources
	cp MacOSX/monodevelop.in $(MAC_APP_DIR)/Contents/MacOS/monodevelop
	cp MacOSX/mdtool.in $(MAC_APP_DIR)/Contents/MacOS/mdtool

