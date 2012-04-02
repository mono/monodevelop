ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Debugger.Soft
ASSEMBLY = $(ADDIN_BUILD)/Mono.Debugging.Soft.dll

SIGNING_KEY=$(top_srcdir)/src/core/Mono.Debugging/mono.debugging.snk

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger.Soft/Mono.Debugger.Soft.dll \
	$(top_builddir)/build/bin/Mono.Cecil.dll \
	$(top_builddir)/build/bin/Mono.Cecil.Mdb.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll

REFS =  \
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
	StringAdaptor.cs \
	VariableValueReference.cs

RES =

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD) $(SIGNING_KEY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -keyfile:$(SIGNING_KEY) -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

check: all

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Debugger.Soft
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

