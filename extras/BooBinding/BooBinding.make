

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=.

include $(top_srcdir)/Makefile.include
include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = booc
ASSEMBLY_COMPILER_FLAGS =  -debug
ASSEMBLY = build/BooBinding.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	build/BooShell.dll
BUILD_DIR = build


endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = booc
ASSEMBLY_COMPILER_FLAGS =  -debug-
ASSEMBLY = build/BooBinding.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	build/BooShell.dll
BUILD_DIR = build


endif

INSTALL_DIR = $(prefix)/lib/monodevelop/AddIns/BooBinding

LINUX_PKGCONFIG = \
	$(BOOBINDING_PC)  



BOOBINDING_PC = $(BUILD_DIR)/monodevelop-boo.pc


FILES = \
	FormattingStrategy/BooFormattingStrategy.boo \
	Gui/ShellTextView.boo \
	Gui/IShellModel.boo \
	Gui/BooShellModel.boo \
	Gui/OptionPanels/CodeCompilationPanel.boo \
	Gui/OptionPanels/GeneralShellPanel.boo \
	Gui/OptionPanels/GeneralBooShellPanel.boo \
	Project/BooCompilerParameters.boo \
	Properties/ShellProperties.boo \
	Properties/BooShellProperties.boo \
	BooBindingCompilerServices.boo \
	BooAmbience.boo \
	BooShellPadContent.boo \
	BooCompiler.boo \
	BooLanguageBinding.boo \
	Parser/BooParser.boo \
	Parser/Resolver.boo \
	Parser/TypeMembers.boo \
	Parser/ExpressionFinder.boo \
	Parser/ReturnType.boo \
	Parser/VariableLookupVisitor.boo \
	Parser/ExpressionTypeVisitor.boo \
	Parser/Tree.boo \
	Parser/Visitor.boo \
	Gui/BooTextEditorExtension.boo 

DATA_FILES = 

RESOURCES = \
	templates/BooGtkSharpProject.xpt.xml \
	templates/BooLibraryProject.xpt.xml \
	templates/BooGtkSharpWindow.xft.xml \
	templates/EmptyBooFile.xft.xml \
	templates/EmptyBooProject.xpt.xml \
	icons/BooBinding.Base \
	icons/Boo.File.EmptyFile \
	icons/Boo.File.Form \
	icons/Boo.FileIcon \
	BooBinding.addin.xml \
	icons/boo-icon-32.png 

EXTRAS = \
	monodevelop-boo.pc.in 

REFERENCES =  \
	System.Xml \
	System.Runtime.Remoting \
	System.Drawing \
	-pkg:boo \
	-pkg:mono-addins \
	-pkg:monodevelop \
	-pkg:monodevelop-core-addins \
	-pkg:gconf-sharp-2.0 \
	-pkg:gtksourceview-sharp-2.0 \
	-pkg:gtk-sharp-2.0

DLL_REFERENCES = 

CLEANFILES += $(LINUX_PKGCONFIG) 

#Targets
all-local: $(ASSEMBLY) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make

$(BOOBINDING_PC): monodevelop-boo.pc
	mkdir -p $(BUILD_DIR)
	cp '$<' '$@'



monodevelop-boo.pc: monodevelop-boo.pc.in $(top_srcdir)/config.make
	sed -e "s,@prefix@,$(prefix)," -e "s,@PACKAGE@,$(PACKAGE)," < monodevelop-boo.pc.in > monodevelop-boo.pc


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

LOCAL_PKGCONFIG=PKG_CONFIG_PATH=../../local-config:$$PKG_CONFIG_PATH

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(dir $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(LOCAL_PKGCONFIG) $(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)


install-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(BOOBINDING_PC)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p $(INSTALL_DIR)
	cp $(ASSEMBLY) $(ASSEMBLY_MDB) $(INSTALL_DIR)
	mkdir -p $(prefix)/lib/pkgconfig
	test -z '$(BOOBINDING_PC)' || cp $(BOOBINDING_PC) $(prefix)/lib/pkgconfig
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(BOOBINDING_PC)
	make pre-uninstall-local-hook prefix=$(prefix)
	rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY))
	test -z '$(ASSEMBLY_MDB)' || rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY_MDB))
	test -z '$(BOOBINDING_PC)' || rm -f $(INSTALL_DIR)/$(notdir $(BOOBINDING_PC))
	make post-uninstall-local-hook prefix=$(prefix)
