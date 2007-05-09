
ADDIN = MonoDevelop.GtkCore.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.GtkCore
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.GtkCore.dll

REFS =  \
	$(GCONF_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:$(top_srcdir)/contrib/Mono.Cecil.dll \
	-r:$(top_srcdir)/Extras/MonoDevelop.GtkCore/lib/libstetic.dll \
	-r:$(top_srcdir)/Extras/MonoDevelop.GtkCore/lib/libsteticui.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	MonoDevelop.GtkCore.Commands/GladeCommands.cs \
	MonoDevelop.GtkCore.Dialogs/BindDesignDialog.cs \
	MonoDevelop.GtkCore.Dialogs/ConfirmWindowDeleteDialog.cs \
	MonoDevelop.GtkCore.Dialogs/GtkFeatureWidget.cs \
	MonoDevelop.GtkCore.Dialogs/SelectRenamedClassDialog.cs \
	MonoDevelop.GtkCore.Dialogs/WidgetBuilderOptionPanel.cs \
	MonoDevelop.GtkCore.GuiBuilder/ActionGroupDisplayBinding.cs \
	MonoDevelop.GtkCore.GuiBuilder/ActionGroupView.cs \
	MonoDevelop.GtkCore.GuiBuilder/ClassUtils.cs \
	MonoDevelop.GtkCore.GuiBuilder/CodeBinder.cs \
	MonoDevelop.GtkCore.GuiBuilder/CombinedDesignView.cs \
	MonoDevelop.GtkCore.GuiBuilder/GtkProjectServiceExtension.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderDisplayBinding.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderPalettePad.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderProject.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderProjectPad.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderService.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderView.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderWindow.cs \
	MonoDevelop.GtkCore.GuiBuilder/PropertiesWidget.cs \
	MonoDevelop.GtkCore.GuiBuilder/ToolboxProvider.cs \
	MonoDevelop.GtkCore.NodeBuilders/ActionGroupNodeBuilder.cs \
	MonoDevelop.GtkCore.NodeBuilders/ProjectFolderNodeBuilderExtension.cs \
	MonoDevelop.GtkCore.NodeBuilders/ProjectNodeBuilder.cs \
	MonoDevelop.GtkCore.NodeBuilders/StockIconsNodeBuilder.cs \
	MonoDevelop.GtkCore.NodeBuilders/WidgetNodeBuilder.cs \
	MonoDevelop.GtkCore.NodeBuilders/WindowsFolder.cs \
	MonoDevelop.GtkCore.NodeBuilders/WindowsFolderNodeBuilder.cs \
	MonoDevelop.GtkCore/GtkCoreService.cs \
	MonoDevelop.GtkCore/GtkDesignInfo.cs \
	MonoDevelop.GtkCore/ProjectResourceProvider.cs \
	MonoDevelop.GtkCore/WidgetFileDescriptionTemplate.cs \
	MonoDevelop.GtkCore/WidgetFileTemplateFullClass.cs \
	MonoDevelop.GtkCore/WidgetFileTemplatePartialClass.cs

RES =  \
	gui.glade \
	icons/actiongroup.png \
	icons/image-x-generic.png \
	icons/widget.png \
	templates/ActionGroup.xft.xml \
	templates/ActionGroupPartial.xft.xml \
	templates/Dialog.xft.xml \
	templates/DialogPartial.xft.xml \
	templates/Widget.xft.xml \
	templates/WidgetPartial.xft.xml \
	templates/Window.xft.xml \
	templates/WindowPartial.xft.xml

STETIC_FILES = \
libstetic.dll \
libsteticui.dll \
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

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $@

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library \
	$(build_resources:%=/resource:%) $(build_sources) $(REFS)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.GtkCore
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN) $(SRC_STETIC_FILES) 

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN) $(BUILD_STETIC_FILES)

include $(top_srcdir)/Makefile.include
