RUNTIME = mono
ASSEMBLY = $(top_builddir)/build/bin/UnitTests.dll

RES = 

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)
NUNIT_CONSOLE = `pkg-config --variable=libdir mono-nunit`/mono/2.0/nunit-console.exe
FIXTURE = /fixture=$(fixture)

REFS =  \
	$(MONO_NUNIT_LIBS) \
	-r:../../build/AddIns/BackendBindings/MonoDevelop.CSharpBinding.dll \
	-r:../../build/AddIns/MonoDevelop.Autotools/MonoDevelop.Autotools.dll \
	-r:../../build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	-r:../../build/bin/MonoDevelop.Core.dll \
	-r:../../build/bin/MonoDevelop.Projects.dll \
	-r:System \
	-r:System.Xml

FILES =  \
	MonoDevelop.Projects/DomCompilationUnitTests.cs \
	MonoDevelop.Projects/MakefileTests.cs \
	MonoDevelop.Projects/MdsTests.cs \
	MonoDevelop.Projects/MonoDeveloperFormatTests.cs \
	MonoDevelop.Projects/MSBuildTests.cs \
	MonoDevelop.Projects/PackagingTests.cs \
	MonoDevelop.Projects/ProjectTests.cs \
	MonoDevelop.Projects/SolutionTests.cs \
	MonoDevelop.Projects/TestProjectsChecks.cs \
	MonoDevelop.Projects/WorkspaceTests.cs \
	TestBase.cs \
	Util.cs

all: $(ASSEMBLY)

test: all
	rm -rf ../tmp
	if test -n "$(fixture)"; then \
		fp="/fixture=$(fixture)"; \
	fi; \
	$(MD_LAUNCH_SETUP) mono --debug $(NUNIT_CONSOLE) $(top_builddir)/build/bin/UnitTests.dll $$fp

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(top_builddir)/build/bin/
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_sources) $(build_resources:%=/resource:%) $(REFS)

include $(top_srcdir)/Makefile.include

