
ASSEMBLY_NAME = MonoDevelop.Visualizers
ADDIN_BUILD = $(top_builddir)/build/AddIns/DebuggerAddIn
ASSEMBLY = $(ADDIN_BUILD)/$(ASSEMBLY_NAME).dll

REFS =	$(GTK_SHARP_LIBS) \
	$(MONO_DEBUGGER_LIBS)

FILES = \
	DialogDebuggerVisualizer.cs \
	IDialogVisualizerService.cs \
	IVisualizerObjectProvider.cs \
	TargetObjectProvider.cs \
	TargetMemoryStream.cs \
	VisualizerDevelopmentHost.cs \
	VisualizerObjectSource.cs

EXTRA_DIST = $(FILES)

DEFINES= -define:NET_2_0 -define:WITH_GTK

if ENABLE_DEBUGGER

noinst_DATA= $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -g $(DEFINES) -keyfile:$(srcdir)/visualizer.snk -out:$@ -target:library \
	$(build_sources) $(REFS)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

install-data-local:
	$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS)

else
all:

endif

include $(top_srcdir)/Makefile.include
