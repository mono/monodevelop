
EXTRA_DIST = Makefile

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=..

include $(top_srcdir)/Makefile.include
include $(top_srcdir)/config.make

#include $(srcdir)/custom-hooks.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = booc
ASSEMBLY_COMPILER_FLAGS =  -debug
ASSEMBLY = ../build/BooShell.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../build


endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = booc
ASSEMBLY_COMPILER_FLAGS =  -debug
ASSEMBLY = ../build/BooShell.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../build


endif

INSTALL_DIR = $(DESTDIR)$(prefix)/lib/monodevelop/AddIns/BooBinding

FILES = \
	BooShell.boo 

DATA_FILES = 

RESOURCES = 

EXTRAS =

REFERENCES =  \
	-pkg:monodevelop \
	-pkg:glib-sharp-2.0 \
	-pkg:gtk-sharp-2.0

DLL_REFERENCES = 

CLEANFILES += $(LINUX_PKGCONFIG) 

#Targets
all: $(ASSEMBLY) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'


LOCAL_PKGCONFIG=PKG_CONFIG_PATH=../../../local-config:$$PKG_CONFIG_PATH

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(dir $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(LOCAL_PKGCONFIG) $(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)


install-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p $(INSTALL_DIR)
	cp $(ASSEMBLY) $(ASSEMBLY_MDB) $(INSTALL_DIR)
	mkdir -p $(DESTDIR)$(prefix)/lib/pkgconfig
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY))
	test -z '$(ASSEMBLY_MDB)' || rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY_MDB))
	make post-uninstall-local-hook prefix=$(prefix)

install: install-local
uninstall: uninstall-local
clean: clean-local

include $(top_srcdir)/rules.make
