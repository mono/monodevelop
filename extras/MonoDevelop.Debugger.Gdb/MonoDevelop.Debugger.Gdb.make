

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=.

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"

ASSEMBLY = build/MonoDevelop.Debugger.Gdb.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build

MONODEVELOP_DEBUGGER_GDB_DLL_MDB_SOURCE=build/MonoDevelop.Debugger.Gdb.dll.mdb
MONODEVELOP_DEBUGGER_GDB_DLL_MDB=$(BUILD_DIR)/MonoDevelop.Debugger.Gdb.dll.mdb

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = build/MonoDevelop.Debugger.Gdb.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build

MONODEVELOP_DEBUGGER_GDB_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(MONODEVELOP_DEBUGGER_GDB_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(MONODEVELOP_DEBUGGER_GDB_PC)  


RESGEN=resgen2

MONODEVELOP_DEBUGGER_GDB_PC = $(BUILD_DIR)/monodevelop.debugger.gdb.pc

FILES =  \
	AssemblyInfo.cs \
	CommandStatus.cs \
	GdbBacktrace.cs \
	GdbCommandResult.cs \
	GdbEvent.cs \
	GdbSession.cs \
	GdbSessionFactory.cs \
	ResultData.cs 

DATA_FILES = 

RESOURCES = Manifest.addin.xml 

EXTRAS = \
	monodevelop.debugger.gdb.pc.in 

REFERENCES =  \
	Mono.Posix \
	-pkg:monodevelop \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

#Targets
all-local: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make



$(eval $(call emit-deploy-wrapper,MONODEVELOP_DEBUGGER_GDB_PC,monodevelop.debugger.gdb.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

INSTALL_DIR = $(DESTDIR)$(prefix)/lib/monodevelop/AddIns/MonoDevelop.Debugger

LOCAL_PKGCONFIG=PKG_CONFIG_PATH=../../local-config:$$PKG_CONFIG_PATH

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(shell dirname $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(LOCAL_PKGCONFIG) $(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)

install-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p '$(INSTALL_DIR)'
	$(call cp,$(ASSEMBLY),$(INSTALL_DIR))
	$(call cp,$(ASSEMBLY_MDB),$(INSTALL_DIR))
	$(call cp,$(MONODEVELOP_DEBUGGER_GDB_DLL_MDB),$(INSTALL_DIR))
	mkdir -p '$(DESTDIR)$(libdir)/pkgconfig'
	$(call cp,$(MONODEVELOP_DEBUGGER_GDB_PC),$(DESTDIR)$(libdir)/pkgconfig)
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(INSTALL_DIR))
	$(call rm,$(ASSEMBLY_MDB),$(INSTALL_DIR))
	$(call rm,$(MONODEVELOP_DEBUGGER_GDB_DLL_MDB),$(INSTALL_DIR))
	$(call rm,$(MONODEVELOP_DEBUGGER_GDB_PC),$(DESTDIR)$(libdir)/pkgconfig)
	make post-uninstall-local-hook prefix=$(prefix)
