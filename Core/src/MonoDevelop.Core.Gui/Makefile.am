
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll

REFS =  \
	$(GECKO_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/bin/Mono.Addins.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.Gui.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.Setup.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:ICSharpCode.SharpZipLib \
	-r:Mono.Posix \
	-r:System \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	Freedesktop.RecentFiles/RecentFileStorage.cs \
	Freedesktop.RecentFiles/RecentItem.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Core.Gui.Dialogs.MultiTaskProgressDialog.cs \
	MonoDevelop.Core.Gui.Codons/DialogPanelCodon.cs \
	MonoDevelop.Core.Gui.Codons/StockIconCodon.cs \
	MonoDevelop.Core.Gui.Components/FileBrowser.cs \
	MonoDevelop.Core.Gui.Components/LocalizedObject.cs \
	MonoDevelop.Core.Gui.Components/LocalizedPropertyAttribute.cs \
	MonoDevelop.Core.Gui.Components/LocalizedPropertyDescriptor.cs \
	MonoDevelop.Core.Gui.Components/MenuButtonEntry.cs \
	MonoDevelop.Core.Gui.Dialogs/AbstractOptionPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/AbstractWizardPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/CurrentPanelPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/DefaultDialogPanelDescriptor.cs \
	MonoDevelop.Core.Gui.Dialogs/ErrorDialog.cs \
	MonoDevelop.Core.Gui.Dialogs/IDialogPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/IDialogPanelDescriptor.cs \
	MonoDevelop.Core.Gui.Dialogs/IWizardPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/MultiTaskProgressDialog.cs \
	MonoDevelop.Core.Gui.Dialogs/ProgressDialog.cs \
	MonoDevelop.Core.Gui.Dialogs/SetupApp.cs \
	MonoDevelop.Core.Gui.Dialogs/StatusPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/TreeViewOptions.cs \
	MonoDevelop.Core.Gui.Dialogs/WizardDialog.cs \
	MonoDevelop.Core.Gui.ErrorHandlers/CombineLoadError.cs \
	MonoDevelop.Core.Gui.ErrorHandlers/GenericError.cs \
	MonoDevelop.Core.Gui.ProgressMonitoring/BaseProgressMonitor.cs \
	MonoDevelop.Core.Gui.ProgressMonitoring/MessageDialogProgressMonitor.cs \
	MonoDevelop.Core.Gui.ProgressMonitoring/MultiTaskDialogProgressMonitor.cs \
	MonoDevelop.Core.Gui.Utils/DesktopApplication.cs \
	MonoDevelop.Core.Gui.Utils/FileIconLoader.cs \
	MonoDevelop.Core.Gui/AsyncDispatchAttribute.cs \
	MonoDevelop.Core.Gui/DispatchService.cs \
	MonoDevelop.Core.Gui/FreeDispatchAttribute.cs \
	MonoDevelop.Core.Gui/GuiService.cs \
	MonoDevelop.Core.Gui/GuiSyncAbstractService.cs \
	MonoDevelop.Core.Gui/GuiSyncContext.cs \
	MonoDevelop.Core.Gui/GuiSyncObject.cs \
	MonoDevelop.Core.Gui/IMementoCapable.cs \
	MonoDevelop.Core.Gui/IMessageService.cs \
	MonoDevelop.Core.Gui/ITextBufferStrategy.cs \
	MonoDevelop.Core.Gui/MessageService.cs \
	MonoDevelop.Core.Gui/PixbufList.cs \
	MonoDevelop.Core.Gui/RecentOpen.cs \
	MonoDevelop.Core.Gui/ResourceNotFoundException.cs \
	MonoDevelop.Core.Gui/ResourceService.cs \
	MonoDevelop.Core.Gui/StockIcons.cs \
	MonoDevelop.Core.Gui/SyncContext.cs \
	MonoDevelop.Core.Gui/SyncContextAttribute.cs \
	MonoDevelop.Core.Gui/SyncObject.cs

RES =  \
	Base.glade \
	gtk-gui/gui.stetic \
	icons/BreakPoint.png \
	icons/comment.png \
	icons/edit-select-all.png \
	icons/edit-select-all_22.png \
	icons/ExecutionMarker.png \
	icons/FileIcons.XmlIcon \
	icons/GeneralWizardBackground \
	icons/gnome-fs-regular.png \
	icons/Icons.16x16.BuildCombine \
	icons/Icons.16x16.BuildCurrentSelectedProject \
	icons/Icons.16x16.Class \
	icons/Icons.16x16.ClearAllBookmarks \
	icons/Icons.16x16.CloseAllDocuments \
	icons/Icons.16x16.CloseCombineIcon \
	icons/Icons.16x16.ClosedFolderBitmap \
	icons/Icons.16x16.ClosedReferenceFolder \
	icons/Icons.16x16.ClosedResourceFolder \
	icons/Icons.16x16.CombineIcon \
	icons/Icons.16x16.Delegate \
	icons/Icons.16x16.Empty \
	icons/Icons.16x16.Enum \
	icons/Icons.16x16.Event \
	icons/Icons.16x16.Field \
	icons/Icons.16x16.FindInFiles \
	icons/Icons.16x16.FindNextIcon \
	icons/Icons.16x16.GotoNextbookmark \
	icons/Icons.16x16.GotoPrevbookmark \
	icons/Icons.16x16.Interface \
	icons/Icons.16x16.InternalClass \
	icons/Icons.16x16.InternalDelegate \
	icons/Icons.16x16.InternalEnum \
	icons/Icons.16x16.InternalEvent \
	icons/Icons.16x16.InternalField \
	icons/Icons.16x16.InternalInterface \
	icons/Icons.16x16.InternalMethod \
	icons/Icons.16x16.InternalProperty \
	icons/Icons.16x16.InternalStruct \
	icons/Icons.16x16.Literal \
	icons/Icons.16x16.Method \
	icons/Icons.16x16.MiscFiles \
	icons/Icons.16x16.NameSpace \
	icons/Icons.16x16.NewCombineIcon.png \
	icons/Icons.16x16.NewFolderIcon \
	icons/Icons.16x16.NewSolutionIcon.png \
	icons/Icons.16x16.OpenFolderBitmap \
	icons/Icons.16x16.OpenProjectIcon \
	icons/Icons.16x16.OpenReferenceFolder \
	icons/Icons.16x16.OpenResourceFolder \
	icons/Icons.16x16.OutputIcon \
	icons/Icons.16x16.PrivateClass \
	icons/Icons.16x16.PrivateDelegate \
	icons/Icons.16x16.PrivateEnum \
	icons/Icons.16x16.PrivateEvent \
	icons/Icons.16x16.PrivateField \
	icons/Icons.16x16.PrivateInterface \
	icons/Icons.16x16.PrivateMethod \
	icons/Icons.16x16.PrivateProperty \
	icons/Icons.16x16.PrivateStruct \
	icons/Icons.16x16.PropertiesIcon \
	icons/Icons.16x16.Property \
	icons/Icons.16x16.ProtectedClass \
	icons/Icons.16x16.ProtectedDelegate \
	icons/Icons.16x16.ProtectedEnum \
	icons/Icons.16x16.ProtectedEvent \
	icons/Icons.16x16.ProtectedField \
	icons/Icons.16x16.ProtectedInterface \
	icons/Icons.16x16.ProtectedMethod \
	icons/Icons.16x16.ProtectedProperty \
	icons/Icons.16x16.ProtectedStruct \
	icons/Icons.16x16.Reference \
	icons/Icons.16x16.ReplaceInFiles \
	icons/Icons.16x16.ResourceFileIcon \
	icons/Icons.16x16.SaveAllIcon \
	icons/Icons.16x16.SolutionIcon \
	icons/Icons.16x16.SplitWindow \
	icons/Icons.16x16.Struct \
	icons/Icons.16x16.TaskListIcon \
	icons/Icons.16x16.TextFileIcon \
	icons/Icons.16x16.TipOfTheDay \
	icons/Icons.16x16.ToggleBookmark \
	icons/Icons.16x16.WebSearchIcon \
	icons/Icons.16x16.XMLFileIcon \
	icons/Icons.24x24.SaveAllIcon \
	icons/Icons.32x32.CombineIcon \
	icons/Icons.32x32.EmptyFileIcon \
	icons/Icons.32x32.EmptyProjectIcon \
	icons/Icons.32x32.HTMLFileIcon \
	icons/Icons.32x32.ResourceFileIcon \
	icons/Icons.32x32.TextFileIcon \
	icons/Icons.32x32.XMLFileIcon \
	icons/Icons.SharpDevelopIcon \
	icons/Icons.SharpDevelopIcon-22.png \
	icons/package-x-generic.png \
	icons/package-x-generic_22.png \
	icons/plugin-16.png \
	icons/plugin-22.png \
	icons/plugin-32.png \
	icons/system-software-update.png \
	icons/system-software-update_22.png \
	icons/uncomment.png \
	icons/user-package.png \
	icons/view-fullscreen.png \
	icons/view-fullscreen_22.png \
	icons/visual-studio.png \
	MonoDevelop.Core.Gui.addin.xml

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
