ASSEMBLY_DIR = $(top_builddir)/bin

ASSEMBLY = $(ASSEMBLY_DIR)/stetic.exe

ASSEMBLY_FILES = $(ASSEMBLY_DIR)/stetic.exe $(ASSEMBLY_DIR)/stetic.exe.mdb

pkglib_DATA = $(ASSEMBLY_FILES)
bin_SCRIPTS = stetic
stetic_exe_sources =  \
	Configuration.cs \
	DesignerView.cs \
	LibraryManagerDialog.cs \
	RecentFiles.cs \
	Stetic.cs \
	UIManager.cs \
	WindowListWidget.cs

	
stetic_exe_resources = stetic.glade                
 
stetic_exe_references =  \
	-pkg:glade-sharp-2.0 \
	-pkg:gnome-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-r:$(top_builddir)/bin/libstetic.dll \
	-r:$(top_builddir)/bin/libsteticui.dll \
	-r:Mono.Posix


comma = ,
RESOURCE_FILES = $(addprefix $(srcdir)/, $(stetic_exe_resources))
RESOURCES = $(foreach file,$(RESOURCE_FILES),-resource:$(file)$(comma)$(notdir $(file)))

all: $(ASSEMBLY)

$(ASSEMBLY): $(stetic_exe_sources) $(stetic_exe_resources) $(top_builddir)/bin/libstetic.dll $(top_builddir)/bin/libsteticui.dll
	$(GMCS) -debug $(stetic_exe_references) -out:$(ASSEMBLY) $(stetic_exe_sources:%=$(srcdir)/%) $(RESOURCES)

stetic: stetic.in
	sed -e "s:\@MONO\@:$(MONO):" -e "s:\@pkglibdir\@:$(pkglibdir):" $< > $@
	chmod a+x $@

EXTRA_DIST = $(stetic_exe_sources) $(RESOURCE_FILES) stetic.in

CLEANFILES = $(ASSEMBLY_FILES) stetic

run:
	$(MONO) --debug $(ASSEMBLY_DIR)/stetic.exe

trace:
	$(MONO) --trace --debug $(ASSEMBLY_DIR)/stetic.exe

mdb:
	mdb $(ASSEMBLY_DIR)/stetic.exe

gdb:
	gdb --args $(MONO) $(ASSEMBLY_DIR)/stetic.exe
