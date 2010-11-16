
ASSEMBLY = $(top_builddir)/build/bin/mdmonitor.exe

FILES =  \
	../../addins/MacPlatform/Framework/AppleEvent.cs \
	../../addins/MacPlatform/Framework/AppleScript.cs \
	../../addins/MacPlatform/Framework/ApplicationEvents.cs \
	../../addins/MacPlatform/Framework/Carbon.cs \
	../../addins/MacPlatform/Framework/ComponentManager.cs \
	../../addins/MacPlatform/Framework/CoreFoundation.cs \
	AssemblyInfo.cs \
	ChartSerieInfo.cs \
	ChartView.cs \
	CounterSelectorDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/Mono.Instrumentation.Monitor.CounterSelectorDialog.cs \
	gtk-gui/Mono.Instrumentation.Monitor.InstrumenationChartView.cs \
	gtk-gui/Mono.Instrumentation.Monitor.InstrumentationViewerDialog.cs \
	gtk-gui/Mono.Instrumentation.Monitor.NewProfile.cs \
	gtk-gui/Mono.Instrumentation.Monitor.TimeLineViewWindow.cs \
	gtk-gui/Mono.Instrumentation.Monitor.TimeStatisticsView.cs \
	InstrumenationChartView.cs \
	InstrumentationViewerDialog.cs \
	MacIntegration/MacIntegration.cs \
	Main.cs \
	NewProfile.cs \
	TimeLineView.cs \
	TimeLineViewWindow.cs \
	TimeStatisticsView.cs

RES = gtk-gui/gui.stetic 

all: $(ASSEMBLY)

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:../../../build/bin/MonoDevelop.Core.dll \
	-r:../../../build/bin/MonoDevelop.Ide.dll \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml
	
$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) /unsafe $(CSC_FLAGS) $(build_sources) $(REFS) $(build_resources:%=/resource:%) -out:$(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb           

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
