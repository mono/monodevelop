
ASSEMBLY = $(top_builddir)/build/bin/mdmonitor.exe

FILES =  \
	AssemblyInfo.cs \
	CounterSelectorDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/Mono.Instrumentation.Monitor.CounterSelectorDialog.cs \
	gtk-gui/Mono.Instrumentation.Monitor.InstrumenationChartView.cs \
	gtk-gui/Mono.Instrumentation.Monitor.InstrumentationViewerDialog.cs \
	gtk-gui/Mono.Instrumentation.Monitor.NewProfile.cs \
	gtk-gui/Mono.Instrumentation.Monitor.TimeLineViewWindow.cs \
	InstrumenationChartView.cs \
	InstrumentationViewerDialog.cs \
	Main.cs \
	NewProfile.cs \
	TimeLineView.cs \
	TimeLineViewWindow.cs

RES = gtk-gui/gui.stetic 

all: $(ASSEMBLY)

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:../../../build/bin/MonoDevelop.Components.dll \
	-r:../../../build/bin/MonoDevelop.Core.dll \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core
	
$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) $(build_sources) $(REFS) $(build_resources:%=/resource:%) -out:$(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb           

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include
