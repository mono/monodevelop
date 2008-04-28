
EXTRA_DIST =  Makefile

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=..

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"

ASSEMBLY = bin/Debug/DebuggerServer.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../DebuggerLibrary/bin/Debug/DebuggerLibrary.dll
BUILD_DIR = bin/Debug/

DEBUGGERLIBRARY_DLL_MDB_SOURCE=../DebuggerLibrary/bin/Debug/DebuggerLibrary.dll.mdb
DEBUGGERLIBRARY_DLL_SOURCE=../DebuggerLibrary/bin/Debug/DebuggerLibrary.dll

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG "-define:TRACE"

ASSEMBLY = bin/Release/DebuggerServer.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../DebuggerLibrary/bin/Release/DebuggerLibrary.dll
BUILD_DIR = bin/Release/

DEBUGGERLIBRARY_DLL_MDB_SOURCE=../DebuggerLibrary/bin/Release/DebuggerLibrary.dll.mdb
DEBUGGERLIBRARY_DLL_SOURCE=../DebuggerLibrary/bin/Release/DebuggerLibrary.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=DebuggerServer.resources.dll

PROGRAMFILES = \
	$(DEBUGGERLIBRARY_DLL_MDB) \
	$(DEBUGGERLIBRARY_DLL)  

BINARIES = \
	$(DEBUGGERSERVER)  


RESGEN=resgen2

DEBUGGERLIBRARY_DLL_MDB = $(BUILD_DIR)/DebuggerLibrary.dll.mdb
DEBUGGERLIBRARY_DLL = $(BUILD_DIR)/DebuggerLibrary.dll
DEBUGGERSERVER = $(BUILD_DIR)/debuggerserver

FILES = \
	Server.cs \
	DebuggerServer.cs \
	BacktraceWrapper.cs \
	AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	ChangeLog \
	debuggerserver.in 

REFERENCES =  \
	System \
	System.Data \
	System.Runtime.Remoting \
	System.Xml \
	-pkg:mono-debugger

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

#Targets
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES)  $(top_srcdir)/config.make

include $(top_srcdir)/Makefile.include
#include $(srcdir)/custom-hooks.make



$(eval $(call emit-deploy-target,DEBUGGERLIBRARY_DLL_MDB))
$(eval $(call emit-deploy-target,DEBUGGERLIBRARY_DLL))
$(eval $(call emit-deploy-wrapper,DEBUGGERSERVER,debuggerserver,x))


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
	$(call cp,$(DEBUGGERLIBRARY_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(DEBUGGERLIBRARY_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	mkdir -p '$(DESTDIR)$(bindir)'
	$(call cp,$(DEBUGGERSERVER),$(DESTDIR)$(bindir))
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DEBUGGERLIBRARY_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DEBUGGERLIBRARY_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DEBUGGERSERVER),$(DESTDIR)$(bindir))
	make post-uninstall-local-hook prefix=$(prefix)

install: install-local
uninstall: uninstall-local
clean: clean-local

include $(top_srcdir)/rules.make
