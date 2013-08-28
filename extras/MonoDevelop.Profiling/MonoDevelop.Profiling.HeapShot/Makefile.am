ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Profiling.HeapShot.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../build/MonoDevelop.Profiling.dll \
	-r:Mono.Posix \
	-r:System

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Profiling.HeapShot.ReferenceTreeViewer.cs \
	Gui/HeapShotProfilingSnapshotNodeBuilder.cs \
	Gui/HeapShotProfilingSnapshotView.cs \
	Gui/ReferenceTreeViewer.cs \
	HeapShotProfiler.cs \
	HeapShotProfilingSnapshot.cs \
	Snapshot/FieldInfo.cs \
	Snapshot/ObjectInfo.cs \
	Snapshot/ObjectMapReader.cs \
	Snapshot/ObjectReference.cs \
	Snapshot/ReferenceNode.cs \
	Snapshot/TypeInfo.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Profiling.HeapShot.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Profiling
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
