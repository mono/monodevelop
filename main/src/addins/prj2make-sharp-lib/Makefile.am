
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.MsVisualStudio
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.MsVisualStudio.dll

REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/AddIns/BackendBindings/MonoDevelop.CSharpBinding.dll \
	-r:$(top_builddir)/build/AddIns/BackendBindings/MonoDevelop.VBNetBinding.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:System \
	-r:System.Xml

FILES =  \
	CSharpMSBuildProjectExtension.cs \
	CSharpResourceIdBuilder.cs \
	csproj_ver2003.cs \
	CsprojInfo.cs \
	DefaultMSBuildProjectExtension.cs \
	IResourceIdBuilder.cs \
	MdPrjHelper.cs \
	MSBuildData.cs \
	MSBuildFileFormat.cs \
	MSBuildProjectExtension.cs \
	MSBuildProjectServiceExtension.cs \
	MsPrjHelper.cs \
	pkgconfiginvoker.cs \
	PrjHelper.cs \
	PrjxInfo.cs \
	sharp_d_cmbx.cs \
	sharp_d_prjx.cs \
	SilverlightMSBuildProjectExtension.cs \
	SlnData.cs \
	SlnFileFormat.cs \
	Utils.cs \
	VBNetMSBuildProjectExtension.cs \
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

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.MsVisualStudio
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

include $(top_srcdir)/Makefile.include

