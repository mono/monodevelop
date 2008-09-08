
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Components.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	gtk-gui/generated.cs \
	MonoDevelop.Components.Chart/Axis.cs \
	MonoDevelop.Components.Chart/AxisDimension.cs \
	MonoDevelop.Components.Chart/AxisPosition.cs \
	MonoDevelop.Components.Chart/BasicChart.cs \
	MonoDevelop.Components.Chart/ChartCursor.cs \
	MonoDevelop.Components.Chart/DateTimeAxis.cs \
	MonoDevelop.Components.Chart/IntegerAxis.cs \
	MonoDevelop.Components.Chart/Serie.cs \
	MonoDevelop.Components.Chart/TickEnumerator.cs \
	MonoDevelop.Components.Commands.ExtensionNodes/CommandCategoryCodon.cs \
	MonoDevelop.Components.Commands.ExtensionNodes/CommandCodon.cs \
	MonoDevelop.Components.Commands.ExtensionNodes/CommandItemCodon.cs \
	MonoDevelop.Components.Commands.ExtensionNodes/ItemSetCodon.cs \
	MonoDevelop.Components.Commands.ExtensionNodes/LinkItemCodon.cs \
	MonoDevelop.Components.Commands.ExtensionNodes/LocalCommandItemCodon.cs \
	MonoDevelop.Components.Commands.ExtensionNodes/SchemeExtensionNode.cs \
	MonoDevelop.Components.Commands.ExtensionNodes/SeparatorItemCodon.cs \
	MonoDevelop.Components.Commands/ActionCommand.cs \
	MonoDevelop.Components.Commands/ActionType.cs \
	MonoDevelop.Components.Commands/Command.cs \
	MonoDevelop.Components.Commands/CommandArrayInfo.cs \
	MonoDevelop.Components.Commands/CommandCheckMenuItem.cs \
	MonoDevelop.Components.Commands/CommandEntry.cs \
	MonoDevelop.Components.Commands/CommandEntrySet.cs \
	MonoDevelop.Components.Commands/CommandErrorHandler.cs \
	MonoDevelop.Components.Commands/CommandFrame.cs \
	MonoDevelop.Components.Commands/CommandHandler.cs \
	MonoDevelop.Components.Commands/CommandHandlerAttribute.cs \
	MonoDevelop.Components.Commands/CommandInfo.cs \
	MonoDevelop.Components.Commands/CommandInfoSet.cs \
	MonoDevelop.Components.Commands/CommandManager.cs \
	MonoDevelop.Components.Commands/CommandMenu.cs \
	MonoDevelop.Components.Commands/CommandMenuBar.cs \
	MonoDevelop.Components.Commands/CommandMenuItem.cs \
	MonoDevelop.Components.Commands/CommandRouterContainer.cs \
	MonoDevelop.Components.Commands/CommandSelectedEventArgs.cs \
	MonoDevelop.Components.Commands/CommandSystemCommands.cs \
	MonoDevelop.Components.Commands/CommandToggleToolButton.cs \
	MonoDevelop.Components.Commands/CommandToolbar.cs \
	MonoDevelop.Components.Commands/CommandToolButton.cs \
	MonoDevelop.Components.Commands/CustomCommand.cs \
	MonoDevelop.Components.Commands/CustomCommandTargetAttribute.cs \
	MonoDevelop.Components.Commands/CustomCommandUpdaterAttribute.cs \
	MonoDevelop.Components.Commands/CustomItem.cs \
	MonoDevelop.Components.Commands/CustomMenuItem.cs \
	MonoDevelop.Components.Commands/ICommandDelegatorRouter.cs \
	MonoDevelop.Components.Commands/ICommandMenuItem.cs \
	MonoDevelop.Components.Commands/ICommandRouter.cs \
	MonoDevelop.Components.Commands/ICommandTargetVisitor.cs \
	MonoDevelop.Components.Commands/ICommandUpdateHandler.cs \
	MonoDevelop.Components.Commands/ICommandUserItem.cs \
	MonoDevelop.Components.Commands/IMultiCastCommandRouter.cs \
	MonoDevelop.Components.Commands/KeyBindingManager.cs \
	MonoDevelop.Components.Commands/KeyBindingService.cs \
	MonoDevelop.Components.Commands/LinkCommandEntry.cs \
	MonoDevelop.Components.Commands/LocalCommandEntry.cs \
	MonoDevelop.Components.Commands/MenuToolButton.cs \
	MonoDevelop.Components.DockToolbars/ArrowWindow.cs \
	MonoDevelop.Components.DockToolbars/DockedPosition.cs \
	MonoDevelop.Components.DockToolbars/DockGrip.cs \
	MonoDevelop.Components.DockToolbars/DockToolbar.cs \
	MonoDevelop.Components.DockToolbars/DockToolbarFrame.cs \
	MonoDevelop.Components.DockToolbars/DockToolbarFrameLayout.cs \
	MonoDevelop.Components.DockToolbars/DockToolbarFrameStatus.cs \
	MonoDevelop.Components.DockToolbars/DockToolbarPanel.cs \
	MonoDevelop.Components.DockToolbars/DockToolbarPosition.cs \
	MonoDevelop.Components.DockToolbars/DockToolbarStatus.cs \
	MonoDevelop.Components.DockToolbars/FixedPanel.cs \
	MonoDevelop.Components.DockToolbars/FloatingDock.cs \
	MonoDevelop.Components.DockToolbars/FloatingPosition.cs \
	MonoDevelop.Components.DockToolbars/IDockBar.cs \
	MonoDevelop.Components.DockToolbars/PlaceholderWindow.cs \
	MonoDevelop.Components/BaseFileEntry.cs \
	MonoDevelop.Components/CellRendererComboBox.cs \
	MonoDevelop.Components/DataGrid.cs \
	MonoDevelop.Components/FileEntry.cs \
	MonoDevelop.Components/FileSelector.cs \
	MonoDevelop.Components/FixedWidthWrapLabel.cs \
	MonoDevelop.Components/FolderDialog.cs \
	MonoDevelop.Components/FolderEntry.cs \
	MonoDevelop.Components/GladeWidgetExtract.cs \
	MonoDevelop.Components/IconView.cs \
	MonoDevelop.Components/InvisibleFrame.cs \
	MonoDevelop.Components/MenuButton.cs \
	MonoDevelop.Components/Navbar.cs \
	MonoDevelop.Components/NotebookButtonBar.cs \
	MonoDevelop.Components/TabLabel.cs \
	MonoDevelop.Components/TreeViewCellContainer.cs \
	MonoDevelop.Components/TreeViewState.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/MonoDevelop.Close.png \
	MonoDevelop.Components.addin.xml

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

