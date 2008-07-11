
EXTRA_DIST =  Makefile

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=..

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"

ASSEMBLY = ../build/DebuggerServer.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = ../build

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG "-define:TRACE"

ASSEMBLY = ../build/DebuggerServer.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = ../build/MonoDevelop.Debugger.Mdb/build

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=DebuggerServer.resources.dll

PROGRAMFILES = \
	$(DEBUGGERSERVER_EXE_MDB)  


RESGEN=resgen2

DEBUGGERSERVER_EXE_MDB = $(BUILD_DIR)/DebuggerServer.exe.mdb

FILES =  \
	ArrayElementGroup.cs \
	ArrayValueReference.cs \
	AssemblyInfo.cs \
	BacktraceWrapper.cs \
	Convert.cs \
	DebuggerServer.cs \
	ExpressionEvaluator.cs \
	FieldReference.cs \
	LiteralValueReference.cs \
	NamespaceValueReference.cs \
	NRefactoryEvaluator.cs \
	NullValueReference.cs \
	PropertyReference.cs \
	RemoteFrameObject.cs \
	Server.cs \
	TypeValueReference.cs \
	Util.cs \
	ValueReference.cs \
	VariableReference.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	ChangeLog 

REFERENCES =  \
	../build/DebuggerClient.dll \
	Mono.Posix \
	-pkg:mono-addins \
	-pkg:mono-debugger \
	-pkg:monodevelop \
	-pkg:monodevelop-core-addins \
	System \
	System.Data \
	System.Runtime.Remoting \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) 

#Targets
all: $(ASSEMBLY) $(PROGRAMFILES)  $(top_srcdir)/config.make

include $(top_srcdir)/Makefile.include
#include $(srcdir)/custom-hooks.make





$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

INSTALL_DIR = $(DESTDIR)$(prefix)/lib/monodevelop/AddIns/MonoDevelop.Debugger

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
	mkdir -p '$(INSTALL_DIR)'
	$(call cp,$(ASSEMBLY),$(INSTALL_DIR))
	$(call cp,$(ASSEMBLY_MDB),$(INSTALL_DIR))
	$(call cp,$(DEBUGGERSERVER_EXE_MDB),$(INSTALL_DIR))
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(INSTALL_DIR))
	$(call rm,$(ASSEMBLY_MDB),$(INSTALL_DIR))
	$(call rm,$(DEBUGGERSERVER_EXE_MDB),$(INSTALL_DIR))
	make post-uninstall-local-hook prefix=$(prefix)

install: install-local
uninstall: uninstall-local
clean: clean-local

include $(top_srcdir)/rules.make
