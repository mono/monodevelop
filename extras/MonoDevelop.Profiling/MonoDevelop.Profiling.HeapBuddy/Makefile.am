ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Profiling.HeapBuddy.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../../../../../install/lib/monodevelop/bin/MonoDevelop.Projects.dll \
	-r:../../../../../install/lib/monodevelop/bin/MonoDevelop.Projects.Gui.dll \
	-r:../build/MonoDevelop.Profiling.dll \
	-r:Mono.Posix \
	-r:System

FILES =  \
	gtk-gui/generated.cs \
	Gui/BacktracesNodeBuilder.cs \
	Gui/BacktracesView.cs \
	Gui/FakeNodes.cs \
	Gui/HeapBuddyProfilingSnapshotNodeBuilder.cs \
	Gui/HistoryNodeBuilder.cs \
	Gui/HistoryView.cs \
	Gui/TypesNodeBuilder.cs \
	Gui/TypesView.cs \
	HeapBuddyProfiler.cs \
	HeapBuddyProfilingSnapshot.cs \
	Snapshot/Backtrace.cs \
	Snapshot/Frame.cs \
	Snapshot/Gc.cs \
	Snapshot/ObjectStats.cs \
	Snapshot/OutfileReader.cs \
	Snapshot/Resize.cs \
	Snapshot/Type.cs \
	Snapshot/Util.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Profiling.HeapBuddy.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Profiling
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
