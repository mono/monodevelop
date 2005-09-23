
DLL = $(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll

REFERENCES = -r:System.Drawing.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Gui.Utils.dll \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS)

FILES = \
IconView/IconView.cs \
FileBrowser/FileBrowser.cs \
FileSelector/FileSelector.cs \
FolderDialog/FolderDialog.cs \
FolderDialog/BaseFileEntry.cs \
FolderDialog/FolderEntry.cs \
FolderDialog/FileEntry.cs \
Navbar/Navbar.cs \
TabLabel/TabLabel.cs \
GladeWidgetExtract/GladeWidgetExtract.cs \
DragNotebook/DragNotebook.cs \
DataGrid/DataGrid.cs \
BasicChart/Axis.cs \
BasicChart/AxisDimension.cs \
BasicChart/AxisPosition.cs \
BasicChart/BasicChart.cs \
BasicChart/ChartCursor.cs \
BasicChart/DateTimeAxis.cs \
BasicChart/IntegerAxis.cs \
BasicChart/Serie.cs \
BasicChart/TickEnumerator.cs \
Commands/ActionCommand.cs \
Commands/ActionType.cs \
Commands/CommandArrayInfo.cs \
Commands/CommandCheckMenuItem.cs \
Commands/Command.cs \
Commands/CommandEntry.cs \
Commands/CommandEntrySet.cs \
Commands/CommandFrame.cs \
Commands/CommandErrorHandler.cs \
Commands/CommandHandlerAttribute.cs \
Commands/CommandHandler.cs \
Commands/CommandInfo.cs \
Commands/CommandManager.cs \
Commands/CommandMenuBar.cs \
Commands/CommandMenu.cs \
Commands/CommandMenuItem.cs \
Commands/CommandRouterContainer.cs \
Commands/CommandSystemCommands.cs \
Commands/CommandToggleToolButton.cs \
Commands/CommandToolbar.cs \
Commands/CommandToolButton.cs \
Commands/CustomCommand.cs \
Commands/CustomMenuItem.cs \
Commands/ICommandDelegatorRouter.cs \
Commands/ICommandMenuItem.cs \
Commands/ICommandRouter.cs \
Commands/ICommandUserItem.cs \
Commands/LinkCommandEntry.cs \
Commands/LocalCommandEntry.cs \
Commands/MenuToolButton.cs \
DockToolbars/ArrowWindow.cs \
DockToolbars/DockGrip.cs \
DockToolbars/DockToolbar.cs \
DockToolbars/DockToolbarFrame.cs \
DockToolbars/DockToolbarFrameLayout.cs \
DockToolbars/DockToolbarFrameStatus.cs \
DockToolbars/DockToolbarPanel.cs \
DockToolbars/DockToolbarPosition.cs \
DockToolbars/DockToolbarStatus.cs \
DockToolbars/DockedPosition.cs \
DockToolbars/FixedPanel.cs \
DockToolbars/FloatingDock.cs \
DockToolbars/FloatingPosition.cs \
DockToolbars/IDockBar.cs \
DockToolbars/PlaceholderWindow.cs

RES = \
icons/MonoDevelop.Close.png

GENERATED_FILES = \
AssemblyInfo.cs

all: $(DLL)

$(DLL): $(build_sources) $(RES)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFERENCES) $(RES:%=/resource:%) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(DLL)

CLEANFILES = $(DLL) $(DLL).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

