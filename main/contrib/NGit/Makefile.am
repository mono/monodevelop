BINDIR=$(top_builddir)/build/bin

all: $(NGIT)

NGIT_DLL=NGit.dll
NGIT=$(BINDIR)/$(NGIT_DLL)
NGIT_DEBUG=$(BINDIR)/$(NGIT_DLL).mdb
NGIT_DIR=$(srcdir)

$(NGIT): $(NGIT_CSFILES)
	mkdir -p $(BINDIR)
	xbuild $(NGIT_DIR)/NGit.csproj

clean-local:
	xbuild /t:Clean $(NGIT_DIR)/NGit.csproj

CLEANFILES = $(NGIT) $(NGIT_MDB)

EXTRA_DIST = \
	     $(wildcard NGit*/*.cs) \
	     $(wildcard resources/*) \
	     AssemblyInfo.cs \
	     NGit.csproj
