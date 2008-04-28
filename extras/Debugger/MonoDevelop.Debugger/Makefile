
EXTRA_DIST =  Makefile

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=..

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug "-define:DEBUG"

ASSEMBLY = bin/Debug/MonoDevelop.Debugger.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../DebuggerClient/bin/Debug/DebuggerClient.dll \
	../DebuggerLibrary/bin/Debug/DebuggerLibrary.dll
BUILD_DIR = bin/Debug/

DEBUGGERCLIENT_DLL_SOURCE=../DebuggerClient/bin/Debug/DebuggerClient.dll
DEBUGGERLIBRARY_DLL_MDB_SOURCE=../DebuggerLibrary/bin/Debug/DebuggerLibrary.dll.mdb
DEBUGGERLIBRARY_DLL_SOURCE=../DebuggerLibrary/bin/Debug/DebuggerLibrary.dll
DEBUGGERCLIENT_DLL_MDB_SOURCE=../DebuggerClient/bin/Debug/DebuggerClient.dll.mdb

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = bin/Release/MonoDevelop.Debugger.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../DebuggerClient/bin/Release/DebuggerClient.dll \
	../DebuggerLibrary/bin/Release/DebuggerLibrary.dll
BUILD_DIR = bin/Release/

DEBUGGERCLIENT_DLL_SOURCE=../DebuggerClient/bin/Release/DebuggerClient.dll
DEBUGGERLIBRARY_DLL_MDB_SOURCE=../DebuggerLibrary/bin/Release/DebuggerLibrary.dll.mdb
DEBUGGERLIBRARY_DLL_SOURCE=../DebuggerLibrary/bin/Release/DebuggerLibrary.dll
DEBUGGERCLIENT_DLL_MDB_SOURCE=../DebuggerClient/bin/Release/DebuggerClient.dll.mdb

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(DEBUGGERCLIENT_DLL) \
	$(DEBUGGERLIBRARY_DLL_MDB) \
	$(DEBUGGERLIBRARY_DLL) \
	$(DEBUGGERCLIENT_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(MONODEVELOP_DEBUGGER_PC)  


RESGEN=resgen2

DEBUGGERCLIENT_DLL = $(BUILD_DIR)/DebuggerClient.dll
DEBUGGERLIBRARY_DLL_MDB = $(BUILD_DIR)/DebuggerLibrary.dll.mdb
DEBUGGERLIBRARY_DLL = $(BUILD_DIR)/DebuggerLibrary.dll
MONODEVELOP_DEBUGGER_PC = $(BUILD_DIR)/monodevelop.debugger.pc
DEBUGGERCLIENT_DLL_MDB = $(BUILD_DIR)/DebuggerClient.dll.mdb

FILES = \
	AssemblyInfo.cs \
	DebuggingService.cs \
	DebugExecutionHandlerFactory.cs \
	Gui/Pads/StackTracePad.cs \
	Services.cs 

DATA_FILES = 

RESOURCES = \
	MonoDevelop.Debugger.addin.xml 

EXTRAS = \
	ChangeLog \
	monodevelop.debugger.pc.in 

REFERENCES =  \
	System \
	-pkg:monodevelop \
	-pkg:mono-addins \
	-pkg:gtk-sharp-2.0

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

#Targets
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make

include $(top_srcdir)/Makefile.include
#include $(srcdir)/custom-hooks.make



$(eval $(call emit-deploy-target,DEBUGGERCLIENT_DLL))
$(eval $(call emit-deploy-target,DEBUGGERLIBRARY_DLL_MDB))
$(eval $(call emit-deploy-target,DEBUGGERLIBRARY_DLL))
$(eval $(call emit-deploy-wrapper,MONODEVELOP_DEBUGGER_PC,monodevelop.debugger.pc))
$(eval $(call emit-deploy-target,DEBUGGERCLIENT_DLL_MDB))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'


$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(shell dirname $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)

install-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p '$(DESTDIR)$(libdir)/$(PACKAGE)'
	$(call cp,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(DEBUGGERCLIENT_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(DEBUGGERLIBRARY_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(DEBUGGERLIBRARY_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	mkdir -p '$(DESTDIR)$(libdir)/pkgconfig'
	$(call cp,$(MONODEVELOP_DEBUGGER_PC),$(DESTDIR)$(libdir)/pkgconfig)
	$(call cp,$(DEBUGGERCLIENT_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DEBUGGERCLIENT_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DEBUGGERLIBRARY_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DEBUGGERLIBRARY_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(MONODEVELOP_DEBUGGER_PC),$(DESTDIR)$(libdir)/pkgconfig)
	$(call rm,$(DEBUGGERCLIENT_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	make post-uninstall-local-hook prefix=$(prefix)

install: install-local
uninstall: uninstall-local
clean: clean-local

include $(top_srcdir)/rules.make
