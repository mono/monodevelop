SUBDIRS = Mono.Addins Mono.Cecil log4net

# We need to copy Firebird to the same directory as where
# mono.data.sql builds. otherwise, mono.data.sql cant find the
# firebird provider when monoquery links against mono.data.sql.
FIREBIRDDIR=$(top_builddir)/build/AddIns/MonoQuery
FIREBIRD_DLL=FirebirdSql.Data.Firebird.dll
FIREBIRD = $(FIREBIRDDIR)/$(FIREBIRD_DLL)

all: $(FIREBIRD)

$(FIREBIRD): $(srcdir)/$(FIREBIRD_DLL)
	mkdir -p $(FIREBIRDDIR)
	cp $(srcdir)/$(FIREBIRD_DLL) $(FIREBIRD)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(FIREBIRD)

CLEANFILES = $(FIREBIRD)
EXTRA_DIST = $(FIREBIRD_DLL) $(srcdir)/FirebirdSql.Data.Firebird.license.txt $(srcdir)/extract_makefile_variable.sh
