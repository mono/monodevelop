

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

INSTALL_DIR = $(DESTDIR)$(prefix)/lib/monodevelop/AddIns/BooBinding

LINUX_PKGCONFIG = \
	$(BOOBINDING_PC)  



BOOBINDING_PC = $(BUILD_DIR)/monodevelop-boo.pc


FILES =  \
	BooAmbience.boo \
	BooBindingCompilerServices.boo \
	BooCompiler.boo \
	BooLanguageBinding.boo \
	BooShellPadContent.boo \
	Gui/BooShellModel.boo \
	Gui/BooTextEditorExtension.boo \
	Gui/IShellModel.boo \
	Gui/OptionPanels/CodeCompilationPanel.boo \
	Gui/OptionPanels/GeneralBooShellPanel.boo \
	Gui/OptionPanels/GeneralShellPanel.boo \
	Gui/ShellTextView.boo \
	Parser/BooParser.boo \
	Parser/ExpressionFinder.boo \
	Parser/ExpressionTypeVisitor.boo \
	Parser/Resolver.boo \
	Parser/ReturnType.boo \
	Parser/Tree.boo \
	Parser/TypeMembers.boo \
	Parser/VariableLookupVisitor.boo \
	Parser/Visitor.boo \
	Project/BooCompilerParameters.boo \
	Properties/BooShellProperties.boo \
	Properties/ShellProperties.boo 

DATA_FILES =  \
	icons/Boo.FileIcon \
	icons/BooBinding.Base 

RESOURCES =  \
	BooBinding.addin.xml \
	icons/Boo.File.EmptyFile \
	icons/Boo.File.Form \
	icons/boo-icon-32.png \
	templates/BooGtkSharpProject.xpt.xml \
	templates/BooGtkSharpWindow.xft.xml \
	templates/BooLibraryProject.xpt.xml \
	templates/EmptyBooFile.xft.xml \
	templates/EmptyBooProject.xpt.xml 

EXTRAS = \
	monodevelop-boo.pc.in 

REFERENCES =  \
	build/BooShell.dll \
	-pkg:boo \
	-pkg:gconf-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:gtksourceview-sharp-2.0 \
	-pkg:mono-addins \
	-pkg:monodevelop \
	-pkg:monodevelop-core-addins \
	System.Drawing \
	System.Runtime.Remoting \
	System.Xml

DLL_REFERENCES = 

DATA_FILE_BUILD = $(addprefix $(BUILD_DIR)/, $(DATA_FILES))
DATA_FILE_INSTALL = $(addprefix $(INSTALL_DIR)/, $(DATA_FILES))

CLEANFILES += $(LINUX_PKGCONFIG) $(DATA_FILE_BUILD)

#Targets
all-local: $(ASSEMBLY) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make $(DATA_FILE_BUILD)

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

$(DATA_FILE_BUILD): $(srcdir)$(subst $(BUILD_DIR),, $@)
	mkdir -p $(dir $@)
	cp $(srcdir)/$(subst $(BUILD_DIR),,$@) $@

$(DATA_FILE_INSTALL): $(srcdir)$(subst $(INSTALL_DIR),, $@)
	mkdir -p $(dir $@)
	cp $(srcdir)/$(subst $(INSTALL_DIR),,$@) $@

install-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(BOOBINDING_PC) $(DATA_FILE_INSTALL)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p $(INSTALL_DIR)
	cp $(ASSEMBLY) $(ASSEMBLY_MDB) $(INSTALL_DIR)
	mkdir -p $(DESTDIR)$(prefix)/lib/pkgconfig
	test -z '$(BOOBINDING_PC)' || cp $(BOOBINDING_PC) $(DESTDIR)$(prefix)/lib/pkgconfig
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(BOOBINDING_PC)
	make pre-uninstall-local-hook prefix=$(prefix)
	rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY))
	rm -f $(DATA_FILE_INSTALL)
	test -z '$(ASSEMBLY_MDB)' || rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY_MDB))
	test -z '$(BOOBINDING_PC)' || rm -f $(INSTALL_DIR)/$(notdir $(BOOBINDING_PC))
	make post-uninstall-local-hook prefix=$(prefix)
