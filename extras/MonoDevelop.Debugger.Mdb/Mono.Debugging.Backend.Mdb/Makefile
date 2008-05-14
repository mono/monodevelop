
EXTRA_DIST =  Makefile

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=..

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"

ASSEMBLY = ../build/DebuggerClient.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../build

DEBUGGERCLIENT_DLL_MDB_SOURCE=../build/DebuggerClient.dll.mdb

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG "-define:TRACE"

ASSEMBLY = bin/Release/DebuggerClient.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release/

DEBUGGERCLIENT_DLL_MDB_SOURCE=bin/Release/DebuggerClient.dll.mdb

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=Mono.Debugging.Backend.Mdb.resources.dll

PROGRAMFILES = \
	$(DEBUGGERCLIENT_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(MONO_DEBUGGING_BACKEND_MDB_PC)  


RESGEN=resgen2

MONO_DEBUGGING_BACKEND_MDB_PC = $(BUILD_DIR)/mono.debugging.backend.mdb.pc
DEBUGGERCLIENT_DLL_MDB = $(BUILD_DIR)/DebuggerClient.dll.mdb

FILES =  \
	AssemblyInfo.cs \
	DebuggerController.cs \
	IDebuggerController.cs \
	MonoDebuggerSession.cs \
	MonoDebuggerSessionFactory.cs 

DATA_FILES = 

RESOURCES = Manifest.addin.xml 

EXTRAS = \
	ChangeLog \
	mono.debugging.backend.mdb.pc.in 

REFERENCES =  \
	-pkg:monodevelop \
	-pkg:monodevelop-core-addins \
	System \
	System.Data \
	System.Runtime.Remoting \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

#Targets
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make

include $(top_srcdir)/Makefile.include
#include $(srcdir)/custom-hooks.make



$(eval $(call emit-deploy-wrapper,MONO_DEBUGGING_BACKEND_MDB_PC,mono.debugging.backend.mdb.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'


LOCAL_PKGCONFIG=PKG_CONFIG_PATH=../../../local-config:$$PKG_CONFIG_PATH

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(shell dirname $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(LOCAL_PKGCONFIG) $(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)

install-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p '$(DESTDIR)$(libdir)/$(PACKAGE)'
	$(call cp,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	mkdir -p '$(DESTDIR)$(libdir)/pkgconfig'
	$(call cp,$(MONO_DEBUGGING_BACKEND_MDB_PC),$(DESTDIR)$(libdir)/pkgconfig)
	$(call cp,$(DEBUGGERCLIENT_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(MONO_DEBUGGING_BACKEND_MDB_PC),$(DESTDIR)$(libdir)/pkgconfig)
	$(call rm,$(DEBUGGERCLIENT_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	make post-uninstall-local-hook prefix=$(prefix)

install: install-local
uninstall: uninstall-local
clean: clean-local

include $(top_srcdir)/rules.make
