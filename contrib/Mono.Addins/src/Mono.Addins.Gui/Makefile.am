
KEY_FILE=$(top_srcdir)/mono-addins.snk
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -keyfile:$(KEY_FILE) -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ../bin/Mono.Addins.Gui.dll
ASSEMBLY_NAME=Mono.Addins.Gui
COMPILE_TARGET = library
PC_FILES_IN = mono-addins-gui.pc.in
PROJECT_REFERENCES =  \
	../bin/Mono.Addins.dll \
	../bin/Mono.Addins.Setup.dll
BUILD_DIR = ../bin


if ENABLE_GUI
all: $(ASSEMBLY)
else
all:
endif

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/Mono.Addins.Gui.AddinInfoDialog.cs \
	gtk-gui/Mono.Addins.Gui.AddinInstallDialog.cs \
	gtk-gui/Mono.Addins.Gui.AddinInstallerDialog.cs \
	gtk-gui/Mono.Addins.Gui.AddinManagerDialog.cs \
	gtk-gui/Mono.Addins.Gui.ErrorDialog.cs \
	gtk-gui/Mono.Addins.Gui.ManageSitesDialog.cs \
	gtk-gui/Mono.Addins.Gui.NewSiteDialog.cs \
	Mono.Addins.Gui/AddinInfoDialog.cs \
	Mono.Addins.Gui/AddinInstallDialog.cs \
	Mono.Addins.Gui/AddinInstaller.cs \
	Mono.Addins.Gui/AddinInstallerDialog.cs \
	Mono.Addins.Gui/AddinManagerDialog.cs \
	Mono.Addins.Gui/AddinManagerWindow.cs \
	Mono.Addins.Gui/AddinTreeWidget.cs \
	Mono.Addins.Gui/ErrorDialog.cs \
	Mono.Addins.Gui/ManageSitesDialog.cs \
	Mono.Addins.Gui/NewSiteDialog.cs \
	Mono.Addins.Gui/Services.cs 

DATA_FILES = 

RESOURCES =  \
	gtk-gui/gui.stetic \
	icons/package-x-generic.png \
	icons/package-x-generic_16.png \
	icons/package-x-generic_22.png \
	icons/system-software-update.png \
	icons/user-package.png 

EXTRAS = \
	./Makefile.am 

REFERENCES =  \
	-pkg:gnome-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-r:Mono.Posix \
	-r:System

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

if ENABLE_GUI
install-data-local:
	echo "$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS)";  \
	$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS) || exit 1;

uninstall-local:
	echo "$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS)"; \
	$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS) || exit 1;

include $(top_srcdir)/Makefile.include
endif
