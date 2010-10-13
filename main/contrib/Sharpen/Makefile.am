BINDIR=$(top_builddir)/build/bin

all: $(SHARPEN)

SHARPEN_DLL=Sharpen.dll
SHARPEN=$(BINDIR)/$(SHARPEN_DLL)
SHARPEN_DEBUG=$(BINDIR)/$(SHARPEN_DLL).mdb
SHARPEN_DIR=$(srcdir)

$(SHARPEN): $(SHARPEN_CSFILES)
	mkdir -p $(BINDIR)
	xbuild $(SHARPEN_DIR)/Sharpen.csproj

CLEANFILES = $(SHARPEN) $(SHARPEN_MDB)


