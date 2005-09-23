
BINDIR=$(top_builddir)/build/bin
LOG4NET_DLL=log4net.dll
LOG4NET = $(BINDIR)/$(LOG4NET_DLL)
CECIL_DLL=Mono.Cecil.dll
CECIL = $(BINDIR)/$(CECIL_DLL)

# We need to copy Firebird to the same directory as where
# mono.data.sql builds. otherwise, mono.data.sql cant find the
# firebird provider when monoquery links against mono.data.sql.
FIREBIRDDIR=$(top_builddir)/build/AddIns/MonoQuery
FIREBIRD_DLL=FirebirdSql.Data.Firebird.dll
FIREBIRD = $(FIREBIRDDIR)/$(FIREBIRD_DLL)

all: $(LOG4NET) $(CECIL) $(FIREBIRD)

$(LOG4NET): $(srcdir)/$(LOG4NET_DLL)
	mkdir -p $(BINDIR)
	cp $(srcdir)/$(LOG4NET_DLL) $(LOG4NET)

$(CECIL): $(srcdir)/$(CECIL_DLL)
	mkdir -p $(BINDIR)
	cp $(srcdir)/$(CECIL_DLL) $(CECIL)

$(FIREBIRD): $(srcdir)/$(FIREBIRD_DLL)
	mkdir -p $(FIREBIRDDIR)
	cp $(srcdir)/$(FIREBIRD_DLL) $(FIREBIRD)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(LOG4NET) $(CECIL) $(FIREBIRD)

CLEANFILES = $(LOG4NET) $(CECIL) $(FIREBIRD)
EXTRA_DIST = $(LOG4NET_DLL) $(CECIL_DLL) $(FIREBIRD_DLL)

