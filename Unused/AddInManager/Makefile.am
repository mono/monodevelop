
ASSEMBLY = AddInManager.dll
ADDIN = AddInManager.addin.xml

REFS = \
	-pkg:gtk-sharp-2.0 \
	/r:../../build/bin/MonoDevelop.Core.dll \
	/r:../../build/bin/MonoDevelop.Base.dll

FILES = \
AssemblyInfo.cs \
Commands/ShowDialog.cs \
Gui/AddInManagerDialog.cs

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN) $(ASSEMBLY)

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES)
	$(CSC) $(CSC_FLAGS) /out:$(ASSEMBLY) /target:library $(FILES) $(REFS) \
	&& cp $(ASSEMBLY) ../../build/AddIns/. \
	&& cp $(ADDIN) ../../build/AddIns/.

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(ADDIN)

