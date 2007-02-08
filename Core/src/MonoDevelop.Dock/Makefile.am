RUNTIME = mono
ASSEMBLY = $(top_builddir)/build/AddIns/MonoDevelop.Dock.dll
TEST = $(top_builddir)/build/AddIns/dock-test.exe

RES = \
	layout.glade \
	stock-close-12.png \
	stock-menu-left-12.png \
	stock-menu-right-12.png


assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)

REFS = -r:System.Xml -r:Mono.Posix.dll $(GTK_SHARP_LIBS) $(GLADE_SHARP_LIBS)

FILES = AssemblyInfo.cs \
	DockedHandler.cs \
	DockMaster.cs \
	Stock.cs \
	DockObjectFlags.cs \
	DockNotebook.cs \
	DockRequest.cs \
	DockBar.cs \
	DockBarButton.cs \
	DockLayout.cs \
	DockPlacement.cs \
	DetachedHandler.cs \
	DockItemGrip.cs \
	DockItem.cs \
	DockPlaceholder.cs \
	DockObject.cs \
	Dock.cs \
	DockPaned.cs \
	DockItemBehavior.cs \
	ExportAttribute.cs \
	AfterAttribute.cs\
	DragNotebook.cs

TEST_FILES = $(srcdir)/GdlDockTest.cs

all: $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(TEST) $(TEST).mdb
EXTRA_DIST = $(FILES) $(RES) $(TEST_FILES)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(top_builddir)/build/bin/
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_sources) $(build_resources:%=/resource:%) $(REFS)

$(TEST) : $(TEST_FILES) $(ASSEMBLY)
	$(CSC) $(CSC_FLAGS) -out:$@ -r:$(ASSEMBLY) -pkg:gtk-sharp-2.0 $(TEST_FILES) -codepage:utf8

run-test: $(TEST)
	$(RUNTIME) $(TEST)

include $(top_srcdir)/Makefile.include

