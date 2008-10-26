

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=.

include $(top_srcdir)/Makefile.include
include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG
ASSEMBLY = build/MonoDevelop.WebKitWebBrowser.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build


endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = build/MonoDevelop.WebKitWebBrowser.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build


endif

INSTALL_DIR = $(DESTDIR)$(prefix)/lib/monodevelop/AddIns/WebKitWebBrowser

LINUX_PKGCONFIG = \
	$(WEBKITWEBBROWSER_PC)  



WEBKITWEBBROWSER_PC = $(BUILD_DIR)/monodevelop-webkitwebbrowser.pc


FILES =  \
	WebKitWebBrowser.cs \
	WebKitWebBrowserLoader.cs

DATA_FILES = 

RESOURCES =  \
	MonoDevelop.WebBrowsers.WebKitWebBrowser.addin.xml 

EXTRAS = \
	ChangeLog \
	monodevelop-webkitwebbrowser.pc.in 

REFERENCES =  \
	-pkg:webkit-sharp-1.0 \
	-pkg:glib-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:monodevelop

DLL_REFERENCES = 

CLEANFILES += $(LINUX_PKGCONFIG) 

#Targets
all-local: $(ASSEMBLY) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make

$(WEBKITWEBBROWSER_PC): monodevelop-webkitwebbrowser.pc
	mkdir -p $(BUILD_DIR)
	cp '$<' '$@'



monodevelop-webkitwebbrowser.pc: monodevelop-webkitwebbrowser.pc.in $(top_srcdir)/config.make
	sed -e "s,@prefix@,$(prefix)," -e "s,@PACKAGE@,$(PACKAGE)," < monodevelop-webkitwebbrowser.pc.in > monodevelop-webkitwebbrowser.pc


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


install-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(WEBKITWEBBROWSER_PC)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p $(INSTALL_DIR)
	cp $(ASSEMBLY) $(ASSEMBLY_MDB) $(INSTALL_DIR)
	mkdir -p $(DESTDIR)$(prefix)/lib/pkgconfig
	test -z '$(WEBKITWEBBROWSER_PC)' || cp $(WEBKITWEBBROWSER_PC) $(DESTDIR)$(prefix)/lib/pkgconfig
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(WEBKITWEBBROWSER_PC)
	make pre-uninstall-local-hook prefix=$(prefix)
	rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY))
	test -z '$(ASSEMBLY_MDB)' || rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY_MDB))
	test -z '$(WEBKITWEBBROWSER_PC)' || rm -f $(DESTDIR)$(prefix)/lib/pkgconfig/$(notdir $(WEBKITWEBBROWSER_PC))
	make post-uninstall-local-hook prefix=$(prefix)
