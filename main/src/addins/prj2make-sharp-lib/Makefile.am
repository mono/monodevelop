
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.MsVisualStudio
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.MsVisualStudio.dll

REFS =  \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/AddIns/BackendBindings/MonoDevelop.CSharpBinding.dll \
	-r:$(top_builddir)/build/AddIns/BackendBindings/MonoDevelop.VBNetBinding.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/NRefactory.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	csproj_ver2003.cs \
	CsprojInfo.cs \
	MdPrjHelper.cs \
	MSBuildProjectServiceExtension.cs \
	MsPrjHelper.cs \
	pkgconfiginvoker.cs \
	PrjHelper.cs \
	PrjxInfo.cs \
	sharp_d_cmbx.cs \
	sharp_d_prjx.cs \
	VS2003ProjectFileFormat.cs

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

