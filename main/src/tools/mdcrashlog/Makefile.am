BINDIR=$(top_builddir)/build/bin

ASSEMBLY=$(BINDIR)/MonoDevelop.CrashLog.dll
PROJECT=MonoDevelop.CrashLog.csproj

REFS =  \
	/r:System \
	/r:System.Web \
	/r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	CrashLogOptions.cs \
	MonoDevelop.SessionLogging/ISessionLogger.cs

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(DEPS) $(srcdir)/$(PROJECT)
	mkdir -p $(BINDIR)
	$(CSC) $(CSC_FLAGS) -target:library -debug $(build_sources) $(REFS) $(build_deps) /out:$(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = \
	$(FILES) \
	$(PROJECT)

include $(top_srcdir)/Makefile.include