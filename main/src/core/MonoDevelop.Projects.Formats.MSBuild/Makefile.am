
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Projects.Formats.MSBuild.exe

REFS =  \
	-r:Microsoft.Build.Engine \
	-r:Microsoft.Build.Framework \
	-r:Microsoft.Build.Utilities.v3.5 \
	-r:System \
	-r:System.Runtime.Remoting

FILES =  \
	AssemblyInfo.cs \
	Main.cs \
	MonoDevelop.Projects.Formats.MSBuild/BuildEngine.cs \
	MonoDevelop.Projects.Formats.MSBuild/ConsoleLogger.cs \
	MonoDevelop.Projects.Formats.MSBuild/IBuildEngine.cs \
	MonoDevelop.Projects.Formats.MSBuild/ILogWriter.cs \
	MonoDevelop.Projects.Formats.MSBuild/IProjectBuilder.cs \
	MonoDevelop.Projects.Formats.MSBuild/LocalLogger.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildResult.cs \
	MonoDevelop.Projects.Formats.MSBuild/ProjectBuilder.cs

RES = 

# Mono's 2.0/3.5 compiler doesn't recognize 3.5 variants of the MSBuild assemblies
# so patch up the references
MSB_REF_PREFIX:=$(shell pkg-config --variable libdir mono)/mono/3.5/
FIXED_REFS:=$(shell echo '$(REFS)' | sed -e 's@Microsoft.Build[^ ]*@$(MSB_REF_PREFIX)&.dll@g')

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	$(CSC) $(CSC_FLAGS) -warnaserror -debug -out:$@ -target:winexe $(FIXED_REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources)
	
$(ASSEMBLY).mdb: $(ASSEMBLY)

$(ASSEMBLY).config: $(srcdir)/app.config
	cp $< $@

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

EXTRA_DIST = $(FILES) $(RES) app.config

include $(top_srcdir)/Makefile.include
