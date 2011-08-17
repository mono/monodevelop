BINDIR=$(top_builddir)/build/bin

all: $(MDCRASHLOG)

MDCRASHLOG_DLL=MonoDevelop.CrashLog.dll
MDCRASHLOG=$(BINDIR)/$(MDCRASHLOG_DLL)
MDCRASHLOG_DEBUG=$(BINDIR)/$(MDCRASHLOG_DLL).mdb
MDCRASHLOG_DIR=$(srcdir)

$(MDCRASHLOG): $(MDCRASHLOG_CSFILES)
	mkdir -p $(BINDIR)
	xbuild $(MDCRASHLOG_DIR)/MonoDevelop.CrashLog.csproj

clean-local:
	xbuild /t:Clean $(MDCRASHLOG_DIR)/MonoDevelop.CrashLog.csproj

CLEANFILES = $(MDCRASHLOG) $(MDCRASHLOG_MDB)

EXTRA_DIST = \
	     $(wildcard MonoDevelop*/*.cs) \
	     $(wildcard resources/*) \
	     AssemblyInfo.cs \
	     MonoDevelop.CrashLog.csproj
