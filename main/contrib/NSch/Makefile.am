BINDIR=$(top_builddir)/build/bin

all: $(NSCH)

NSCH_DLL=NSch.dll
NSCH=$(BINDIR)/$(NSCH_DLL)
NSCH_DEBUG=$(BINDIR)/$(NSCH_DLL).mdb
NSCH_DIR=$(srcdir)

$(NSCH): $(NSCH_CSFILES)
	mkdir -p $(BINDIR)
	xbuild $(NSCH_DIR)/NSch.csproj

CLEANFILES = $(NSCH) $(NSCH_MDB)


