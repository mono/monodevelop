
ADDIN_BUILD = $(top_builddir)/build/AddIns
ASSEMBLY = $(ADDIN_BUILD)/MacPlatform.dll

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml \
	-r:System.Xml.Linq

FILES =  \
	AssemblyInfo.cs \
	Commands.cs \
	Framework/AppleEvent.cs \
	Framework/AppleScript.cs \
	Framework/ApplicationEvents.cs \
	Framework/Carbon.cs \
	Framework/ComponentManager.cs \
	Framework/CoreFoundation.cs \
	Framework/HIToolbox.cs \
	Framework/NavDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Platform.Updater.UpdateDialog.cs \
	MacPlatform.cs \
	MacSelectFileDialogHandler.cs \
	Menus/OSXMenu.cs \
	Updater/Commands.cs \
	Updater/UpdateDialog.cs \
	Updater/UpdateResult.cs \
	Updater/UpdateService.cs

RES =  \
	gtk-gui/gui.stetic \
	MacPlatform.addin.xml

if ENABLE_MACPLATFORM
all: $(ASSEMBLY) $(ASSEMBLY).mdb
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -unsafe $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

if ENABLE_MACPLATFORM
assemblydir = $(MD_ADDIN_DIR)/MacPlatform
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
