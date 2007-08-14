
ADDIN_BUILD = $(top_builddir)/build/AddIns/prj2makesharp
ASSEMBLY = $(ADDIN_BUILD)/prj2make-sharp-lib.dll

REFS =  \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/BackendBindings/CSharpBinding.dll \
	-r:$(top_builddir)/build/AddIns/BackendBindings/VBNetBinding.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:System \
	-r:System.Xml

FILES =  \
	CSharpResourceIdBuilder.cs \
	csproj_ver2003.cs \
	CsprojInfo.cs \
	IResourceIdBuilder.cs \
	MdPrjHelper.cs \
	MSBuildData.cs \
	MSBuildFileFormat.cs \
	MSBuildProject.cs \
	MSBuildProjectServiceExtension.cs \
	MSBuildSolution.cs \
	MsPrjHelper.cs \
	pkgconfiginvoker.cs \
	PrjHelper.cs \
	PrjxInfo.cs \
	sharp_d_cmbx.cs \
	sharp_d_prjx.cs \
	SlnData.cs \
	SlnFileFormat.cs \
	VBNetResourceIdBuilder.cs \
	VS2003ProjectFileFormat.cs \
	VS2003SlnFileFormat.cs

RES = prj2make-sharp-lib.addin.xml

GENERATED_FILES = \
AssemblyInfo.cs

EXTRA_DIST = $(FILES) $(RES)

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_resources:%=/resource:%) $(build_sources) $(REFS)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

include $(top_srcdir)/Makefile.include

