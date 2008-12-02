SUBDIRS = libstetic libsteticui

ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.GtkCore
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.GtkCore.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.GtkCore/libstetic.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.GtkCore/libsteticui.dll \
	$(top_builddir)/build/bin/Mono.Cecil.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
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
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderDocumentOutline.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderProject.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderService.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderView.cs \
	MonoDevelop.GtkCore.GuiBuilder/GuiBuilderWindow.cs \
	MonoDevelop.GtkCore.GuiBuilder/PropertiesWidget.cs \
	MonoDevelop.GtkCore.GuiBuilder/ToolboxLoader.cs \
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
	MonoDevelop.GtkCore/ObjectsDocument.cs \
	MonoDevelop.GtkCore/ProjectResourceProvider.cs \
	MonoDevelop.GtkCore/ReferenceManager.cs \
	MonoDevelop.GtkCore/WidgetFileDescriptionTemplate.cs \
	MonoDevelop.GtkCore/WidgetParser.cs

RES =  \
	gtk-gui/gui.stetic \
	gui.glade \
	icons/actiongroup.png \
	icons/image-x-generic.png \
	icons/widget.png \
	MonoDevelop.GtkCore.addin.xml \
	templates/ActionGroup.xft.xml \
	templates/ActionGroupPartial.xft.xml \
	templates/Dialog.xft.xml \
	templates/DialogPartial.xft.xml \
	templates/Widget.xft.xml \
	templates/WidgetPartial.xft.xml \
	templates/Window.xft.xml \
	templates/WindowPartial.xft.xml


EXTRA_DIST = $(FILES) $(RES)

update-stetic:
	svn rm -m "Update Stetic sources" svn+ssh://mono-cvs.ximian.com/source/trunk/monodevelop/main/src/addins/MonoDevelop.GtkCore/lib/stetic && \
	svn cp -m "Update Stetic sources" svn+ssh://mono-cvs.ximian.com/source/trunk/stetic svn+ssh://mono-cvs.ximian.com/source/trunk/monodevelop/main/src/addins/MonoDevelop.GtkCore/lib/stetic

all: $(ASSEMBLY)

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library \
	$(build_resources:%=/resource:%) $(build_sources) $(REFS) $(build_deps)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.GtkCore
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

include $(top_srcdir)/Makefile.include
