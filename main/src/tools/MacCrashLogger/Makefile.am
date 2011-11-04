APP_NAME = MonoDevelopLogAgent
APP_BUNDLE = $(top_builddir)/build/bin/$(APP_NAME).app
APP_BUNDLE_MACOS = $(APP_BUNDLE)/Contents/MacOS
APP_BUNDLE_RESOURCES = $(APP_BUNDLE)/Contents/Resources

ASSEMBLY = $(APP_BUNDLE_RESOURCES)/$(APP_NAME).exe

FILES =  \
	AppDelegate.cs \
	Main.cs \
	MainMenu.xib.designer.cs

REFS =  \
	/r:../../../../extras/MonoDevelop.MonoMac/MonoDevelop.MonoMac/MonoMac.dll \
	/r:System \
	/r:System.Core \
	/r:System.Drawing \
	/r:System.Xml \
	/r:System.Xml.Linq

DEPS = ../../../build/bin/MonoDevelop.Core.dll

BUNDLE_INFOPLIST = $(APP_BUNDLE)/Contents/Info.plist
BUNDLE_MAINNIB = $(APP_BUNDLE_RESOURCES)/MainMenu.nib
BUNDLE_SCRIPT = $(APP_BUNDLE_MACOS)/$(APP_NAME)
BUNDLE_MONOMAC = $(APP_BUNDLE_RESOURCES)/MonoMac.dll
BUNDLE_CRASHLOG = $(APP_BUNDLE_RESOURCES)/MonoDevelop.CrashLog.dll
BUNDLE_MONO = $(APP_BUNDLE)/$(APP_NAME)

INFOPLIST = $(srcdir)/Info.plist
MAINXIB = $(srcdir)/MainMenu.xib
SCRIPT = $(srcdir)/MonoMacLaunchScript.sh

if ENABLE_MACPLATFORM
all: $(ASSEMBLY) $(BUNDLE_INFOPLIST) $(BUNDLE_MAINNIB) $(BUNDLE_SCRIPT) $(BUNDLE_MONOMAC) $(BUNDLE_MONO) $(BUNDLE_CRASHLOG)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(DEPS)
	mkdir -p $(APP_BUNDLE_RESOURCES)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) $(build_deps) /out:$(ASSEMBLY)

$(BUNDLE_MAINNIB): $(MAINXIB)
	mkdir -p $(APP_BUNDLE_RESOURCES)
	ibtool "$^" --compile "$@"

$(BUNDLE_INFOPLIST): $(INFOPLIST)
	mkdir -p $(APP_BUNDLE_MACOS)
	cp  "$^" "$@"

$(BUNDLE_SCRIPT): $(SCRIPT)
	mkdir -p $(APP_BUNDLE_MACOS)
	cp  "$^" "$@"
	chmod +x "$@"

$(BUNDLE_MONO):
	mkdir -p $(APP_BUNDLE_MACOS)
	ln -s /Library/Frameworks/Mono.framework/Versions/Current/bin/mono "$@"

$(BUNDLE_MONOMAC):
	mkdir -p $(APP_BUNDLE_RESOURCES)
	cd $(APP_BUNDLE_RESOURCES) \
		&& ln -s ../../../../AddIns/MonoMac.dll MonoMac.dll \
		&& ln -s ../../../../AddIns/MonoMac.dll.mdb MonoMac.dll.mdb

$(BUNDLE_CRASHLOG):
	mkdir -p $(APP_BUNDLE_RESOURCES)
	cd $(APP_BUNDLE_RESOURCES) \
		&& ln -s ../../../MonoDevelop.CrashLog.dll MonoDevelop.CrashLog.dll \
		&& ln -s ../../../MonoDevelop.CrashLog.dll.mdb MonoDevelop.CrashLog.dll.mdb
	

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(BUNDLE_INFOPLIST) $(BUNDLE_MAINXIB)
EXTRA_DIST = $(FILES) $(INFOPLIST) $(MAINXIB)

include $(top_srcdir)/Makefile.include