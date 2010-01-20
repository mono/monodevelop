ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Debugger.Soft
ASSEMBLY = $(ADDIN_BUILD)/Mono.Debugging.Soft.dll

DBG_ASM = $(ADDIN_BUILD)/Mono.Debugger.Soft.dll
DBG_ASM_SRC = $(top_srcdir)/src/addins/MonoDevelop.Debugger.Soft/Mono.Debugging.Soft/Mono.Debugger.Soft.dll
DBG_MDB = $(DBG_ASM).mdb
DBG_MDB_SRC = $(DBG_ASM_SRC).mdb

SIGNING_KEY=$(top_srcdir)/src/core/Mono.Debugging/mono.debugging.snk

DEPS =  \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	../../../../build/bin/Mono.Debugging.dll

REFS =  \
	-r:$(top_srcdir)/src/addins/MonoDevelop.Debugger.Soft/Mono.Debugging.Soft/Mono.Debugger.Soft.dll \
	-r:Mono.Debugger.Soft.dll \
	-r:System \
	-r:System.Core

FILES =  \
	ArrayAdaptor.cs \
	AssemblyInfo.cs \
	FieldValueReference.cs \
	LoggingService.cs \
	PropertyValueReference.cs \
	SoftDebuggerAdaptor.cs \
	SoftDebuggerBacktrace.cs \
	SoftDebuggerSession.cs \
	SoftDebuggerStartInfo.cs \
	SoftEvaluationContext.cs \
	VariableValueReference.cs

RES =

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD) $(SIGNING_KEY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS) $(DBG_ASM) $(DBG_MDB)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -keycontainer:$(SIGNING_KEY) -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(DBG_ASM): $(DBG_ASM_SRC)
	mkdir -p $(ADDIN_BUILD)
	cp $^ $@ 

$(DBG_MDB): $(DBG_MDB_SRC)
	mkdir -p $(ADDIN_BUILD)
	cp $^ $@

$(ASSEMBLY).mdb: $(ASSEMBLY)

check: all

assemblydir = $(MD_ADDIN_DIR)/Mono.Debugging.Soft
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DBG_ASM) $(DBG_MDB)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DBG_ASM) $(DBG_MDB)
EXTRA_DIST = $(FILES) $(RES) $(DBG_ASM_SRC) $(DBG_MDB_SRC)

include $(top_srcdir)/Makefile.include

