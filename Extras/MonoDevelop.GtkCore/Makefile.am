
ADDIN = MonoDevelop.GtkCore.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.GtkCore
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.GtkCore.dll

DLLS = $(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	-r:lib/libstetic.dll \
	-r:lib/libsteticui.dll \
	-pkg:gconf-sharp-2.0 \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/Mono.Cecil.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll

FILES = \
MonoDevelop.GtkCore/GtkCoreService.cs \
MonoDevelop.GtkCore/GtkDesignInfo.cs \
MonoDevelop.GtkCore/GeneratorBuildStep.cs \
MonoDevelop.GtkCore/WidgetFileDescriptionTemplate.cs \
MonoDevelop.GtkCore.WidgetLibrary/AssemblyReferenceWidgetLibrary.cs \
MonoDevelop.GtkCore.WidgetLibrary/BaseWidgetLibrary.cs \
MonoDevelop.GtkCore.WidgetLibrary/ProjectClassDescriptor.cs \
MonoDevelop.GtkCore.WidgetLibrary/ProjectPropertyDescriptor.cs \
MonoDevelop.GtkCore.WidgetLibrary/ProjectSignalDescriptor.cs \
MonoDevelop.GtkCore.WidgetLibrary/ProjectWidgetLibrary.cs \
MonoDevelop.GtkCore.Dialogs/WidgetBuilderOptionPanel.cs \
MonoDevelop.GtkCore.Dialogs/SelectRenamedClassDialog.cs \
MonoDevelop.GtkCore.Dialogs/BindDesignDialog.cs \
MonoDevelop.GtkCore.Dialogs/ConfirmWindowDeleteDialog.cs \
MonoDevelop.GtkCore.Commands/GladeCommands.cs \
MonoDevelop.GtkCore.NodeBuilders/ProjectNodeBuilder.cs \
MonoDevelop.GtkCore.NodeBuilders/ProjectFolderNodeBuilderExtension.cs \
MonoDevelop.GtkCore.NodeBuilders/WidgetNodeBuilder.cs \
MonoDevelop.GtkCore.NodeBuilders/WindowsFolderNodeBuilder.cs \
MonoDevelop.GtkCore.NodeBuilders/WindowsFolder.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderPalettePad.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderProjectPad.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderPropertiesPad.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderService.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderProject.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderWindow.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderView.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderDisplayBinding.cs \
MonoDevelop.GtkCore.GuiBuilder/GuiBuilderEditSession.cs \
MonoDevelop.GtkCore.GuiBuilder/ClassUtils.cs \
MonoDevelop.GtkCore.GuiBuilder/WidgetTreeCombo.cs \
MonoDevelop.GtkCore.GuiBuilder/GladeFileDisplayBinding.cs

RES = \
gui.glade \
icons/dialog.png \
icons/widget.png \
icons/window.png \
templates/Window.xft.xml \
templates/Widget.xft.xml \
templates/Dialog.xft.xml

STETIC_FILES = \
libstetic.dll \
libstetic.dll.mdb \
libsteticui.dll \
libsteticui.dll.mdb \
libstetic.dll.config \
libsteticui.dll.config

STETIC_LIBDIR := $(shell pkg-config --variable=libdir stetic)/stetic/

LIBDIR_STETIC_FILES = $(addprefix $(STETIC_LIBDIR), $(STETIC_FILES))
SRC_STETIC_FILES = $(addprefix lib/, $(STETIC_FILES))
BUILD_STETIC_FILES = $(addprefix $(ADDIN_BUILD)/, $(STETIC_FILES))

EXTRA_DIST = $(FILES) $(ADDIN) $(RES) $(SRC_STETIC_FILES)

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(BUILD_STETIC_FILES)

update-stetic:
	mkdir -p lib
	cp $(LIBDIR_STETIC_FILES) lib 

build_sources = $(addprefix $(srcdir)/, $(FILES))

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $@

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library \
	$(RES:%=/resource:%) $(build_sources) $(DLLS)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.GtkCore
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN) $(SRC_STETIC_FILES) 

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN) $(BUILD_STETIC_FILES)
