RUNTIME = mono
DOCK = $(top_builddir)/build/bin/MonoDevelop.Dock.dll
TEST = $(top_builddir)/build/bin/dock-test.exe

GLADE = layout.glade

IMAGES = stock-close-12.png stock-menu-left-12.png stock-menu-right-12.png

RESOURCES = \
	-resource:$(srcdir)/$(GLADE),$(GLADE) \
	-resource:$(srcdir)/stock-close-12.png,stock-close-12.png \
	-resource:$(srcdir)/stock-menu-left-12.png,stock-menu-left-12.png \
	-resource:$(srcdir)/stock-menu-right-12.png,stock-menu-right-12.png

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(DOCK)

REFS = $(GTK_SHARP_LIBS) $(GLADE_SHARP_LIBS)

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
	AfterAttribute.cs

TEST_FILES = $(srcdir)/GdlDockTest.cs

all: $(DOCK)

CLEANFILES = $(DOCK) $(DOCK).mdb $(TEST) $(TEST).mdb
EXTRA_DIST = $(FILES) $(GLADE) $(IMAGES) $(TEST_FILES)

$(DOCK): $(build_sources) $(GLADE) $(IMAGES)
	mkdir -p $(top_builddir)/build/bin/
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_sources) $(RESOURCES) $(REFS)

$(TEST) : $(TEST_FILES) $(DOCK)
	$(CSC) $(CSC_FLAGS) -out:$@ -r:$(DOCK) -pkg:gtk-sharp-2.0 $(TEST_FILES) -codepage:utf8

run-test: $(TEST)
	$(RUNTIME) $(TEST)

include $(top_srcdir)/Makefile.include

