
ADDIN_BUILD = $(top_builddir)/build/AddIns
ASSEMBLY = $(ADDIN_BUILD)/MacPlatform.dll

MONOMAC_SRC = $(top_srcdir)/../extras/MonoDevelop.MonoMac/MonoDevelop.MonoMac/MonoMac.dll
MONOMAC = $(ADDIN_BUILD)/MonoMac.dll

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:../../../../extras/MonoDevelop.MonoMac/MonoDevelop.MonoMac/MonoMac.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:System.Web \
	-r:System.Xml \
	-r:System.Xml.Linq

FILES =  \
	AssemblyInfo.cs \
	Commands.cs \
	Dialogs/Layout.cs \
	Dialogs/MacAddFileDialogHandler.cs \
	Dialogs/MacAlertDialogHandler.cs \
	Dialogs/MacOpenFileDialogHandler.cs \
	Dialogs/MacSelectFileDialogHandler.cs \
	Dialogs/MDBox.cs \
	Dialogs/SelectEncodingPanel.cs \
	Dialogs/SelectEncodingPopUpButton.cs \
	ExternalConsoleProcess.cs \
	Framework/AppleEvent.cs \
	Framework/AppleScript.cs \
	Framework/ApplicationEvents.cs \
	Framework/Carbon.cs \
	Framework/ComponentManager.cs \
	Framework/CoreFoundation.cs \
	Framework/HIToolbox.cs \
	MacPlatform.cs \
	Menus/OSXMenu.cs

RES = MacPlatform.addin.xml

if ENABLE_MACPLATFORM
all: $(MONOMAC) $(MONOMAC).mdb $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -unsafe $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

$(MONOMAC).mdb: $(MONOMAC_SRC).mdb
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(MONOMAC): $(MONOMAC_SRC)
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

if ENABLE_MACPLATFORM
assemblydir = $(MD_ADDIN_DIR)/MacPlatform
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(MONOMAC) $(MONOMAC).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(MONOMAC) $(MONOMAC).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
