
BINDIR=$(top_builddir)/build/bin
LOG4NET_DLL=log4net.dll
LOG4NET = $(BINDIR)/$(LOG4NET_DLL)
CECIL_DLL=Mono.Cecil.dll
CECIL = $(BINDIR)/$(CECIL_DLL)
MONO_ADDINS_DLL=Mono.Addins.dll
MONO_ADDINS = $(BINDIR)/$(MONO_ADDINS_DLL)
MONO_ADDINS_SETUP_DLL=Mono.Addins.Setup.dll
MONO_ADDINS_SETUP = $(BINDIR)/$(MONO_ADDINS_SETUP_DLL)
MONO_ADDINS_GUI_DLL=Mono.Addins.Gui.dll
MONO_ADDINS_GUI = $(BINDIR)/$(MONO_ADDINS_GUI_DLL)

# We need to copy Firebird to the same directory as where
# mono.data.sql builds. otherwise, mono.data.sql cant find the
# firebird provider when monoquery links against mono.data.sql.
FIREBIRDDIR=$(top_builddir)/build/AddIns/MonoQuery
FIREBIRD_DLL=FirebirdSql.Data.Firebird.dll
FIREBIRD = $(FIREBIRDDIR)/$(FIREBIRD_DLL)

all: $(LOG4NET) $(CECIL) $(FIREBIRD) $(MONO_ADDINS) $(MONO_ADDINS_SETUP) $(MONO_ADDINS_GUI)

$(LOG4NET): $(srcdir)/$(LOG4NET_DLL)
	mkdir -p $(BINDIR)
	cp $(srcdir)/$(LOG4NET_DLL) $(LOG4NET)

$(CECIL): $(srcdir)/$(CECIL_DLL)
	mkdir -p $(BINDIR)
	cp $(srcdir)/$(CECIL_DLL) $(CECIL)

$(MONO_ADDINS): $(srcdir)/$(MONO_ADDINS_DLL) $(srcdir)/$(MONO_ADDINS_DLL).config
	mkdir -p $(BINDIR)
	cp $(srcdir)/$(MONO_ADDINS_DLL) $(MONO_ADDINS)
	cp $(srcdir)/$(MONO_ADDINS_DLL).config $(MONO_ADDINS).config

$(MONO_ADDINS_GUI): $(srcdir)/$(MONO_ADDINS_GUI_DLL)
	mkdir -p $(BINDIR)
	cp $(srcdir)/$(MONO_ADDINS_GUI_DLL) $(MONO_ADDINS_GUI)

$(MONO_ADDINS_SETUP): $(srcdir)/$(MONO_ADDINS_SETUP_DLL)
	mkdir -p $(BINDIR)
	cp $(srcdir)/$(MONO_ADDINS_SETUP_DLL) $(MONO_ADDINS_SETUP)

$(FIREBIRD): $(srcdir)/$(FIREBIRD_DLL)
	mkdir -p $(FIREBIRDDIR)
	cp $(srcdir)/$(FIREBIRD_DLL) $(FIREBIRD)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(LOG4NET) $(CECIL) $(FIREBIRD) $(MONO_ADDINS) $(MONO_ADDINS).config $(MONO_ADDINS_SETUP) $(MONO_ADDINS_GUI)

CLEANFILES = $(LOG4NET) $(CECIL) $(FIREBIRD) $(MONO_ADDINS) $(MONO_ADDINS).config
EXTRA_DIST = $(LOG4NET_DLL) $(CECIL_DLL) $(FIREBIRD_DLL) $(MONO_ADDINS_DLL) $(MONO_ADDINS_DLL).config $(MONO_ADDINS_SETUP_DLL) $(MONO_ADDINS_GUI_DLL)

