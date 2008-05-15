	
ASSEMBLY_BUILD_DIR = $(top_builddir)/build/bin
ASSEMBLY = $(ASSEMBLY_BUILD_DIR)/Mono.Debugging.dll
  

REFS =  \
	$(MONO_ADDINS_LIBS) \
	-r:System

FILES =  \
	Mono.Debugging.Backend/DebuggerExtensionNode.cs \
	Mono.Debugging.Backend/IBacktrace.cs \
	Mono.Debugging.Backend/IDebuggerSessionBackend.cs \
	Mono.Debugging.Backend/IDebuggerSessionFactory.cs \
	Mono.Debugging.Backend/IDebuggerSessionFrontend.cs \
	Mono.Debugging.Backend/IObjectValueSource.cs \
	Mono.Debugging.Client/Backtrace.cs \
	Mono.Debugging.Client/Breakpoint.cs \
	Mono.Debugging.Client/BreakpointEventArgs.cs \
	Mono.Debugging.Client/BreakpointStore.cs \
	Mono.Debugging.Client/DebuggerEngine.cs \
	Mono.Debugging.Client/DebuggerSession.cs \
	Mono.Debugging.Client/DebuggerStartInfo.cs \
	Mono.Debugging.Client/ObjectPath.cs \
	Mono.Debugging.Client/ObjectValue.cs \
	Mono.Debugging.Client/ObjectValueKind.cs \
	Mono.Debugging.Client/ProcessEventArgs.cs \
	Mono.Debugging.Client/SourceLocation.cs \
	Mono.Debugging.Client/StackFrame.cs \
	Mono.Debugging.Client/TargetEventArgs.cs \
	Mono.Debugging.Client/TargetEventType.cs \
	Mono.Debugging.Client/ThreadEventArgs.cs

RES = Mono.Debugging.addin.xml 

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ASSEMBLY_BUILD_DIR)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
