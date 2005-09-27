
ADDIN = prj2make-sharp-lib.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/prj2makesharp
ASSEMBLY = $(ADDIN_BUILD)/prj2make-sharp-lib.dll

DLLS = $(GTK_SHARP_LIBS) \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll


FILES = \
MsPrjHelper.cs \
PrjHelper.cs \
MdPrjHelper.cs \
CsprojInfo.cs \
PrjxInfo.cs \
pkgconfiginvoker.cs \
sharp_d_cmbx.cs \
sharp_d_prjx.cs \
csproj_ver2003.cs \
prj2make-sharp-lib-Commands.cs

GENERATED_FILES = \
AssemblyInfo.cs

EXTRA_DIST = $(FILES) $(ADDIN)

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN):
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_sources) $(DLLS)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

include $(top_srcdir)/Makefile.include

