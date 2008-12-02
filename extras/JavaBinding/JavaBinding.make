

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=.

include $(top_srcdir)/Makefile.include
include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug -define:DEBUG
ASSEMBLY = build/JavaBinding.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build


endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = build/JavaBinding.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build


endif

INSTALL_DIR = $(DESTDIR)$(prefix)/lib/monodevelop/AddIns/JavaBinding

LINUX_PKGCONFIG = \
	$(JAVABINDING_PC)  



JAVABINDING_PC = $(BUILD_DIR)/monodevelop-java.pc


FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/JavaBinding.CodeGenerationPanelWidget.cs \
	gtk-gui/JavaBinding.GlobalOptionsPanelWidget.cs \
	Gui/GlobalOptionsPanel.cs \
	Gui/ProjectConfigurationPropertyPanel.cs \
	IKVMCompilerManager.cs \
	JavaCompiler.cs \
	JavaLanguageBinding.cs \
	Project/JavaCompilerParameters.cs 

DATA_FILES = 

RESOURCES =  \
	gtk-gui/gui.stetic \
	icons/Java.FileIcon \
	icons/java-16.png \
	icons/java-22.png \
	icons/java-icon-32.png \
	JavaBinding.addin.xml \
	md1format.xml \
	templates/EmptyJavaFile.xft.xml \
	templates/EmptyJavaProject.xpt.xml \
	templates/IkvmConsoleApplicationProject.xpt.xml \
	templates/IkvmGladeApplicationProject.xpt.xml \
	templates/IkvmGnomeApplicationProject.xpt.xml \
	templates/IkvmGtkApplicationProject.xpt.xml \
	templates/IkvmLibraryProject.xpt.xml \
	templates/JavaApplet.xft.xml \
	templates/JavaApplication.xft.xml \
	templates/JavaApplicationProject.xpt.xml \
	templates/JavaConsoleApplicationProject.xpt.xml \
	templates/JavaDialog.xft.xml \
	templates/JavaFrame.xft.xml \
	templates/JavaOKDialog.xft.xml \
	templates/JavaPanel.xft.xml 

EXTRAS = \
	monodevelop-java.pc.in 

REFERENCES =  \
	Mono.Posix \
	-pkg:glade-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:mono-addins \
	-pkg:monodevelop \
	System \
	System.Drawing \
	System.Xml

DLL_REFERENCES = 

CLEANFILES += $(LINUX_PKGCONFIG) 

#Targets
all-local: $(ASSEMBLY) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make

$(JAVABINDING_PC): monodevelop-java.pc
	mkdir -p $(BUILD_DIR)
	cp '$<' '$@'



monodevelop-java.pc: monodevelop-java.pc.in $(top_srcdir)/config.make
	sed -e "s,@prefix@,$(prefix)," -e "s,@PACKAGE@,$(PACKAGE)," < monodevelop-java.pc.in > monodevelop-java.pc


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

update-po:
	mdtool gettext-update

LOCAL_PKGCONFIG=PKG_CONFIG_PATH=../../local-config:$$PKG_CONFIG_PATH

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(dir $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(LOCAL_PKGCONFIG) $(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)


install-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(JAVABINDING_PC)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p $(INSTALL_DIR)
	cp $(ASSEMBLY) $(ASSEMBLY_MDB) $(INSTALL_DIR)
	mkdir -p $(DESTDIR)$(prefix)/lib/pkgconfig
	test -z '$(JAVABINDING_PC)' || cp $(JAVABINDING_PC) $(DESTDIR)$(prefix)/lib/pkgconfig
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(JAVABINDING_PC)
	make pre-uninstall-local-hook prefix=$(prefix)
	rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY))
	test -z '$(ASSEMBLY_MDB)' || rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY_MDB))
	test -z '$(JAVABINDING_PC)' || rm -f $(INSTALL_DIR)/$(notdir $(JAVABINDING_PC))
	make post-uninstall-local-hook prefix=$(prefix)
