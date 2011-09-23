include $(top_srcdir)/Makefile.include

BINDIR=$(top_builddir)/build/bin
ASSEMBLY=$(BINDIR)/MacCrashLogger.exe
SOLUTION_FILE=$(srcdir)/../../../Main.sln

all:
	mkdir -p $(BINDIR)
	$(MDTOOL_RUN) build $(SOLUTION_FILE) -p:MacCrashLogger

clean-local:
	rm -f $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = \
	     $(wildcard MonoDevelop*/*.cs) \
	     AssemblyInfo.cs \
	     MonoDevelop.CrashLog.csproj


