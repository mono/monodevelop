BUILD_DIR=$(top_builddir)/build/AddIns/MonoDevelop.GtkCore

all: $(libstetic) $(libsteticui)

libstetic: $(libstetic)
libsteticui: $(libsteticui)

### Common settings ###
MCS = gmcs
EXTRACT_VARS = $(top_srcdir)/contrib/extract_makefile_variable.sh


### libstetic ###

libstetic_DIR = $(srcdir)/stetic/libstetic
libstetic_DLL = libstetic.dll
libstetic = $(BUILD_DIR)/$(libstetic_DLL)
libstetic_MAKEFILE = $(srcdir)/stetic/libstetic/Makefile.am

libstetic_FLAGS = -debug -target:library

libstetic_SOURCES := $(addprefix $(libstetic_DIR)/,$(shell sh $(EXTRACT_VARS) $(libstetic_MAKEFILE) "libstetic_dll_sources"))
libstetic_RESOURCES := $(addprefix $(libstetic_DIR)/,$(shell sh $(EXTRACT_VARS) $(libstetic_MAKEFILE) "libstetic_dll_resources"))
libstetic_REFERENCES := $(shell sh $(EXTRACT_VARS) $(libstetic_MAKEFILE) "libstetic_dll_references")
libstetic_DATAFILES := $(shell sh $(EXTRACT_VARS) $(libstetic_MAKEFILE) "libstetic_dll_datafiles")
libstetic_DIST := $(addprefix $(libstetic_DIR)/,$(libstetic_DATAFILES)) $(libstetic_MAKEFILE) $(libstetic_RESOURCES) $(libstetic_SOURCES)

$(libstetic) $(libstetic).mdb: $(libstetic_SOURCES) $(libstetic_RESOURCES) $(libstetic_DATAFILES_BUILD) 
	mkdir -p $(BUILD_DIR)
	$(MCS) $(libstetic_FLAGS) -out:$@ $(libstetic_SOURCES) $(libstetic_REFERENCES) $(libstetic_RESOURCES:%=-resource:%)

libstetic_DATAFILES_BUILD = $(addprefix $(BUILD_DIR)/, $(libstetic_DATAFILES))
  
$(libstetic_DATAFILES_BUILD): $(BUILD_DIR)/% : $(addprefix $(libstetic_DIR)/, %)
	mkdir -p $(dir $@)		
	cp $< $@


### libsteticui ###

libsteticui_DIR = $(srcdir)/stetic/libsteticui
libsteticui_DLL = libsteticui.dll
libsteticui = $(BUILD_DIR)/$(libsteticui_DLL)
libsteticui_MAKEFILE = $(srcdir)/stetic/libsteticui/Makefile.am

libsteticui_FLAGS = -debug -unsafe -main:Stetic.ApplicationBackend

libsteticui_SOURCES := $(addprefix $(libsteticui_DIR)/,$(shell sh $(EXTRACT_VARS) $(libsteticui_MAKEFILE) "libsteticui_dll_sources"))
libsteticui_RESOURCES := $(addprefix $(libsteticui_DIR)/,$(shell sh $(EXTRACT_VARS) $(libsteticui_MAKEFILE) "libsteticui_dll_resources"))
libsteticui_DATAFILES := $(shell sh $(EXTRACT_VARS) $(libsteticui_MAKEFILE) "libsteticui_dll_datafiles")
libsteticui_DIST := $(addprefix $(libsteticui_DIR)/,$(libsteticui_DATAFILES)) $(libsteticui_MAKEFILE) $(libsteticui_RESOURCES) $(libsteticui_SOURCES)

# fails because makefile has relative references
#libsteticui_REFERENCES := $(shell sh $(EXTRACT_VARS) $(libsteticui_MAKEFILE) "libsteticui_dll_references")
libsteticui_REFERENCES  =  \
	-pkg:glib-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-r:$(libstetic) \
	-r:$(top_builddir)/build/bin/Mono.Cecil.dll \
	-r:Mono.Posix \
	-r:System.Runtime.Remoting \
	-r:System.Xml

$(libsteticui) $(libsteticui).mdb: $(libsteticui_SOURCES) $(libsteticui_RESOURCES) $(libsteticui_DATAFILES_BUILD) 
	mkdir -p $(BUILD_DIR)
	$(MCS) $(libsteticui_FLAGS) -out:$@ $(libsteticui_SOURCES) $(libsteticui_REFERENCES) $(libsteticui_RESOURCES:%=-resource:%)

libsteticui_DATAFILES_BUILD = $(addprefix $(BUILD_DIR)/, $(libsteticui_DATAFILES))
  
$(libsteticui_DATAFILES_BUILD): $(BUILD_DIR)/% : $(addprefix $(libsteticui_DIR)/, %)
	mkdir -p $(dir $@)		
	cp $< $@


### Dist ###

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.GtkCore
assembly_DATA = $(libstetic) $(libsteticui) $(libstetic).mdb $(libsteticui).mdb $(libstetic_DATAFILES_BUILD) $(libsteticui_DATAFILES_BUILD)

CLEANFILES = $(assembly_DATA)
EXTRA_DIST = $(libstetic_DIST) $(libsteticui_DIST)
