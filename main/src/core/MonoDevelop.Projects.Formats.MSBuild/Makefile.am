
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Projects.Formats.MSBuild.exe

REFS =  \
	-r:Microsoft.Build.Engine \
	-r:Microsoft.Build.Framework \
	-r:Microsoft.Build.Utilities \
	-r:System \
	-r:System.Runtime.Remoting

FILES =  \
	AssemblyInfo.cs \
	Main.cs \
	MonoDevelop.Projects.Formats.MSBuild/ILogWriter.cs \
	MonoDevelop.Projects.Formats.MSBuild/IProjectBuilder.cs \
	MonoDevelop.Projects.Formats.MSBuild/LocalLogger.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildResult.cs \
	MonoDevelop.Projects.Formats.MSBuild/ProjectBuilder.cs

RES = 

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	$(CSC) $(CSC_FLAGS) -warnaserror -debug -out:$@ -target:winexe $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources)
	
$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
