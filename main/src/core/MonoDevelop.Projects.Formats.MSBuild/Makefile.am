FILES =  \
	AssemblyInfo.cs \
	AssemblyInfo.v2.0.cs \
	AssemblyInfo.v3.5.cs \
	AssemblyInfo.v4.0.cs \
	Main.cs \
	MonoDevelop.Projects.Formats.MSBuild/BuildEngine.cs \
	MonoDevelop.Projects.Formats.MSBuild/ConsoleLogger.cs \
	MonoDevelop.Projects.Formats.MSBuild/IBuildEngine.cs \
	MonoDevelop.Projects.Formats.MSBuild/ILogWriter.cs \
	MonoDevelop.Projects.Formats.MSBuild/IProjectBuilder.cs \
	MonoDevelop.Projects.Formats.MSBuild/LocalLogger.cs \
	MonoDevelop.Projects.Formats.MSBuild/MSBuildResult.cs \
	MonoDevelop.Projects.Formats.MSBuild/ProjectBuilder.cs

PROJECTS = \
	MonoDevelop.Projects.Formats.MSBuild.csproj \
	MonoDevelop.Projects.Formats.MSBuild.v2.0.csproj \
	MonoDevelop.Projects.Formats.MSBuild.v3.5.csproj

REFS = 
RES = 

all:
	for p in $(PROJECTS); do xbuild $$p; done

clean-local:
	for p in $(PROJECTS); do xbuild /t:Clean $$p; done

ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Projects.Formats.MSBuild.exe
assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY20 = $(top_builddir)/build/bin/MSBuild/2.0/MonoDevelop.Projects.Formats.MSBuild.exe
assembly20dir = $(MD_ASSEMBLY_DIR)\MSBuild\2.0
assembly20_DATA = $(ASSEMBLY20) $(ASSEMBLY20).mdb $(ASSEMBLY20).config

ASSEMBLY35 = $(top_builddir)/build/bin/MSBuild/3.5/MonoDevelop.Projects.Formats.MSBuild.exe
assembly35dir = $(MD_ASSEMBLY_DIR)\MSBuild\3.5
assembly35_DATA = $(ASSEMBLY35) $(ASSEMBLY35).mdb $(ASSEMBLY35).config

.PHONY: $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config $(ASSEMBLY20) $(ASSEMBLY20).mdb $(ASSEMBLY20).config $(ASSEMBLY35) $(ASSEMBLY35).mdb $(ASSEMBLY35).config

EXTRA_DIST = $(FILES) \
	$(PROJECTS) \
	app.config \
	MonoDevelop.Projects.Formats.MSBuild.v2.0.exe.config \
	MonoDevelop.Projects.Formats.MSBuild.v3.5.exe.config

include $(top_srcdir)/Makefile.include

