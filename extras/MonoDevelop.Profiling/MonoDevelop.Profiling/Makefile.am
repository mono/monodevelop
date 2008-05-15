ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Profiling.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONODEVELOP_CORE_ADDINS_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	AbstractProfiler.cs \
	AbstractProfilerSnapshot.cs \
	Codons/ProfilerCodon.cs \
	Commands/AbstractProfilerList.cs \
	Commands/PadCommands.cs \
	Commands/ProjectCommands.cs \
	Commands/ToolCommands.cs \
	Dialogs/SelectExecutableDialog.cs \
	Dialogs/SelectProcessDialog.cs \
	DummyProcessAsyncOperation.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Profiling.SelectExecutableDialog.cs \
	gtk-gui/MonoDevelop.Profiling.SelectProcessDialog.cs \
	IProfiler.cs \
	IProfilingSnapshot.cs \
	MonoProfilerExecutionHandler.cs \
	Pads/ProfilingPad.cs \
	Pads/ProfilingSnapshotCollectionNodeBuilder.cs \
	ProcessProfilerExecutionHandler.cs \
	ProfilerEventArgs.cs \
	ProfilerState.cs \
	ProfilerStateEventArgs.cs \
	ProfilingContext.cs \
	ProfilingOperations.cs \
	ProfilingService.cs \
	ProfilingSnapshotCollection.cs \
	ProfilingSnapshotEventArgs.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/Icons.16x16.Profiling.Filter.png \
	icons/Icons.16x16.Profiling.GC.png \
	icons/Icons.16x16.Profiling.History.png \
	icons/Icons.16x16.Profiling.Snapshot.png \
	icons/Icons.16x16.Profiling.Summary.png \
	MonoDevelop.Profiling.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Profiling
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
