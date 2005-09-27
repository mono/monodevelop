
ASSEMBLY_NAME = MonoDevelop.Visualizers
ADDIN_BUILD = $(top_builddir)/build/AddIns/DebuggerAddIn
ASSEMBLY = $(ADDIN_BUILD)/$(ASSEMBLY_NAME).dll

DLLS = $(GTK_SHARP_LIBS) \
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

noinst_DATA= $(ASSEMBLY)

build_sources = $(addprefix $(srcdir)/, $(FILES))

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -g $(DEFINES) -keyfile:$(srcdir)/visualizer.snk -out:$@ -target:library \
	$(build_sources) $(DLLS)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

install-data-local:
	$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS)

else
all:

endif
