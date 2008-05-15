RUNTIME = mono
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Dock.dll

RES =  \
	layout.glade \
	stock-auto-hide.png \
	stock-close-12.png \
	stock-dock.png \
	stock-menu-left-12.png \
	stock-menu-right-12.png


assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	AutoHideBox.cs \
	DockBar.cs \
	DockBarItem.cs \
	DockContainer.cs \
	DockFrame.cs \
	DockFrameTopLevel.cs \
	DockGroup.cs \
	DockGroupItem.cs \
	DockGroupType.cs \
	DockItem.cs \
	DockItemBehavior.cs \
	DockItemContainer.cs \
	DockItemStatus.cs \
	DockLayout.cs \
	DockObject.cs \
	DockPosition.cs \
	PlaceholderWindow.cs \
	TabStrip.cs

all: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(top_builddir)/build/bin/
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_sources) $(build_resources:%=/resource:%) $(REFS)

include $(top_srcdir)/Makefile.include

