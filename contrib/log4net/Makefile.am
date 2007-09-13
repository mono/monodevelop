BINDIR=$(top_builddir)/build/bin

all: $(LOG4NET)

LOG4NET_DLL=log4net.dll
LOG4NET = $(BINDIR)/$(LOG4NET_DLL)
LOG4NET_DIR=$(srcdir)/src
LOG4NET_CSFILES=$(addprefix $(srcdir)/, $(shell find $(LOG4NET_DIR) -name *.cs))
LOG4NET_DIST= $(LOG4NET_CSFILES)
LOG4NET_REFS=/r:System /r:System.Data /r:System.Web

$(LOG4NET): $(LOG4NET_CSFILES)
	mkdir -p $(BINDIR)
	mcs /target:library /out:$(LOG4NET) $(LOG4NET_REFS) $(LOG4NET_CSFILES)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(LOG4NET)

CLEANFILES =  $(assembly_DATA)
EXTRA_DIST = $(LOG4NET_DIST) log4net.license.txt

