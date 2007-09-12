BINDIR=$(top_builddir)/build/bin

all: $(MONO_ADDINS) $(MONO_ADDINS_SETUP) $(MONO_ADDINS_GUI) 

### Common settings ###
KEY_FILE = $(srcdir)/src/mono-addins.snk
MCS = mcs


### Mono.Addins ###

MONO_ADDINS_DIR = $(srcdir)/src/Mono.Addins
MONO_ADDINS_DLL = Mono.Addins.dll
MONO_ADDINS = $(BINDIR)/$(MONO_ADDINS_DLL)
MONO_ADDINS_MAKEFILE = $(srcdir)/src/Mono.Addins/Makefile.am

MONO_ADDINS_FLAGS = -keyfile:$(KEY_FILE) -langversion:ISO-1 -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG "-main:Mono.Addins.Database.SetupProcess" -target:exe

MONO_ADDINS_FILES := $(addprefix $(MONO_ADDINS_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/files.sed $(MONO_ADDINS_MAKEFILE)))
MONO_ADDINS_RESOURCES := $(addprefix $(MONO_ADDINS_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/resources.sed $(MONO_ADDINS_MAKEFILE)))
MONO_ADDINS_REFERENCES := $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/references.sed $(MONO_ADDINS_MAKEFILE))
MONO_ADDINS_DATA_FILES := $(addprefix $(MONO_ADDINS_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/data_files.sed $(MONO_ADDINS_MAKEFILE)))
MONO_ADDINS_DIST := $(MONO_ADDINS_FILES)  $(MONO_ADDINS_RESOURCES) $(MONO_ADDINS_DATA_FILES) $(MONO_ADDINS_MAKEFILE) $(MONO_ADDINS_DIR)/$(MONO_ADDINS_DLL).config

$(MONO_ADDINS): $(MONO_ADDINS_FILES) $(MONO_ADDINS_RESOURCES) $(MONO_ADDINS_DATA_FILES) $(MONO_ADDINS).config 
	mkdir -p $(BINDIR)
	$(MCS) $(MONO_ADDINS_FLAGS) -out:$@ $(MONO_ADDINS_FILES) $(MONO_ADDINS_REFERENCES) $(MONO_ADDINS_RESOURCES:%=-resource:%)

$(MONO_ADDINS).config:
	mkdir -p $(BINDIR)
	cp $(MONO_ADDINS_DIR)/$(MONO_ADDINS_DLL).config $(MONO_ADDINS).config


### Mono.Addins.Setup ###

MONO_ADDINS_SETUP_DIR = $(srcdir)/src/Mono.Addins.Setup
MONO_ADDINS_SETUP_DLL = Mono.Addins.Setup.dll
MONO_ADDINS_SETUP = $(BINDIR)/$(MONO_ADDINS_SETUP_DLL)
MONO_ADDINS_SETUP_MAKEFILE = $(srcdir)/src/Mono.Addins.Setup/Makefile.am

MONO_ADDINS_SETUP_FLAGS = -keyfile:$(KEY_FILE) -langversion:ISO-1 -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG -target:library

MONO_ADDINS_SETUP_FILES := $(addprefix $(MONO_ADDINS_SETUP_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/files.sed $(MONO_ADDINS_SETUP_MAKEFILE)))
MONO_ADDINS_SETUP_RESOURCES := $(addprefix $(MONO_ADDINS_SETUP_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/resources.sed $(MONO_ADDINS_SETUP_MAKEFILE)))
MONO_ADDINS_SETUP_REFERENCES := $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/references.sed $(MONO_ADDINS_SETUP_MAKEFILE))
MONO_ADDINS_SETUP_DATA_FILES := $(addprefix $(MONO_ADDINS_SETUP_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/data_files.sed $(MONO_ADDINS_SETUP_MAKEFILE)))
MONO_ADDINS_SETUP_DIST := $(MONO_ADDINS_SETUP_FILES)  $(MONO_ADDINS_SETUP_RESOURCES) $(MONO_ADDINS_SETUP_DATA_FILES) $(MONO_ADDINS_SETUP_MAKEFILE)

$(MONO_ADDINS_SETUP): $(MONO_ADDINS_SETUP_FILES) $(MONO_ADDINS_SETUP_RESOURCES) $(MONO_ADDINS_SETUP_DATA_FILES) $(MONO_ADDINS)
	mkdir -p $(BINDIR)
	$(MCS) $(MONO_ADDINS_SETUP_FLAGS) -out:$@ $(MONO_ADDINS_SETUP_FILES) $(MONO_ADDINS_SETUP_REFERENCES) $(MONO_ADDINS_SETUP_RESOURCES:%=-resource:%) -r:$(MONO_ADDINS)


### Mono.Addins.Gui ###

MONO_ADDINS_GUI_DIR = $(srcdir)/src/Mono.Addins.Gui
MONO_ADDINS_GUI_DLL = Mono.Addins.Gui.dll
MONO_ADDINS_GUI = $(BINDIR)/$(MONO_ADDINS_GUI_DLL)
MONO_ADDINS_GUI_MAKEFILE = $(srcdir)/src/Mono.Addins.Gui/Makefile.am

MONO_ADDINS_GUI_FLAGS = -keyfile:$(KEY_FILE) -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG -target:library

MONO_ADDINS_GUI_FILES := $(addprefix $(MONO_ADDINS_GUI_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/files.sed $(MONO_ADDINS_GUI_MAKEFILE)))
MONO_ADDINS_GUI_RESOURCES := $(addprefix $(MONO_ADDINS_GUI_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/resources.sed $(MONO_ADDINS_GUI_MAKEFILE)))
MONO_ADDINS_GUI_REFERENCES := $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/references.sed $(MONO_ADDINS_GUI_MAKEFILE))
MONO_ADDINS_GUI_DATA_FILES := $(addprefix $(MONO_ADDINS_GUI_DIR)/, $(shell sed -f $(srcdir)/singlelinevars.sed -f $(srcdir)/data_files.sed $(MONO_ADDINS_GUI_MAKEFILE)))
MONO_ADDINS_GUI_DIST := $(MONO_ADDINS_GUI_FILES)  $(MONO_ADDINS_GUI_RESOURCES) $(MONO_ADDINS_GUI_DATA_FILES) $(MONO_ADDINS_GUI_MAKEFILE)

$(MONO_ADDINS_GUI): $(MONO_ADDINS_GUI_FILES) $(MONO_ADDINS_GUI_RESOURCES) $(MONO_ADDINS_GUI_DATA_FILES) $(MONO_ADDINS) $(MONO_ADDINS_SETUP)
	mkdir -p $(BINDIR)
	$(MCS) $(MONO_ADDINS_GUI_FLAGS) -out:$@ $(MONO_ADDINS_GUI_FILES) $(MONO_ADDINS_GUI_REFERENCES) $(MONO_ADDINS_GUI_RESOURCES:%=-resource:%) -r:$(MONO_ADDINS) -r:$(MONO_ADDINS_SETUP)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(MONO_ADDINS) $(MONO_ADDINS_SETUP) $(MONO_ADDINS_GUI) $(MONO_ADDINS).config

CLEANFILES = $(assembly_DATA)
EXTRA_DIST = $(MONO_ADDINS_DIST) $(MONO_ADDINS_SETUP_DIST) $(MONO_ADDINS_GUI_DIST) $(KEY_FILE) singlelinevars.sed resources.sed files.sed data_files.sed references.sed 

