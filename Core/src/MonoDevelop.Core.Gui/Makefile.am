
ASSEMBLY = $(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll

REFS = \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:System \
	-r:System.Xml \
	-r:System.Drawing \
	-r:ICSharpCode.SharpZipLib.dll \
	$(GTK_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GECKO_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS)

FILES= \
MonoDevelop.Core.Gui/IMessageService.cs \
MonoDevelop.Core.Gui.Components/FileBrowser.cs \
MonoDevelop.Core.Gui.Utils/FileIconLoader.cs \
MonoDevelop.Core.Gui/MessageService.cs \
MonoDevelop.Core.Gui/ResourceNotFoundException.cs \
MonoDevelop.Core.Gui.Dialogs/ResourceService.cs \
MonoDevelop.Core.Gui/StockIcons.cs \
MonoDevelop.Core.Gui/AsyncDispatchAttribute.cs \
MonoDevelop.Core.Gui/DispatchService.cs \
MonoDevelop.Core.Gui/FreeDispatchAttribute.cs \
MonoDevelop.Core.Gui/GuiSyncAbstractService.cs \
MonoDevelop.Core.Gui/GuiSyncContext.cs \
MonoDevelop.Core.Gui/GuiSyncObject.cs \
MonoDevelop.Core.Gui/SyncContext.cs \
MonoDevelop.Core.Gui/SyncContextAttribute.cs \
MonoDevelop.Core.Gui/SyncObject.cs \
MonoDevelop.Core.Gui/DefaultFileService.cs \
MonoDevelop.Core.Gui/FileEventArgs.cs \
MonoDevelop.Core.Gui/IFileService.cs \
Freedesktop.RecentFiles/RecentFiles.cs \
MonoDevelop.Core.Gui/RecentOpen.cs \
MonoDevelop.Core.Gui.Dialogs/DefaultDialogPanelDescriptor.cs \
MonoDevelop.Core.Gui.Codons/DialogPanelCodon.cs \
MonoDevelop.Core.Gui.Dialogs/IDialogPanel.cs \
MonoDevelop.Core.Gui.Dialogs/IDialogPanelDescriptor.cs \
MonoDevelop.Core.Gui.ProgressMonitoring/BaseProgressMonitor.cs \
MonoDevelop.Core.Gui/CommandService.cs \
MonoDevelop.Core.Gui/GuiService.cs \
MonoDevelop.Core.Gui/IMementoCapable.cs \
MonoDevelop.Core.Gui/PixbufList.cs \
MonoDevelop.Core.Gui.Components/LocalizedObject.cs \
MonoDevelop.Core.Gui.Components/LocalizedPropertyAttribute.cs \
MonoDevelop.Core.Gui.Components/LocalizedPropertyDescriptor.cs \
MonoDevelop.Core.Gui.Dialogs/AbstractOptionPanel.cs \
MonoDevelop.Core.Gui.Dialogs/ErrorDialog.cs \
MonoDevelop.Core.Gui.Dialogs/TreeViewOptions.cs \
MonoDevelop.Core.Gui.Dialogs/AbstractWizardPanel.cs \
MonoDevelop.Core.Gui.Dialogs/CurrentPanelPanel.cs \
MonoDevelop.Core.Gui.Dialogs/IWizardPanel.cs \
MonoDevelop.Core.Gui.Dialogs/StatusPanel.cs \
MonoDevelop.Core.Gui.Dialogs/WizardDialog.cs \
MonoDevelop.Core.Gui.Dialogs/AddinManagerDialog.cs \
MonoDevelop.Core.Gui.Dialogs/AddinInstallDialog.cs \
MonoDevelop.Core.Gui.Dialogs/AddinTreeWidget.cs \
MonoDevelop.Core.Gui.Dialogs/AddinInfoDialog.cs \
MonoDevelop.Core.Gui.Dialogs/ManageSitesDialog.cs \
MonoDevelop.Core.Gui.Dialogs/NewSiteDialog.cs \
MonoDevelop.Core.Gui.Dialogs/ProgressDialog.cs \
MonoDevelop.Core.Gui.ErrorHandlers/CombineLoadError.cs \
MonoDevelop.Core.Gui.ErrorHandlers/GenericError.cs \
MonoDevelop.Core.Gui/ITextBufferStrategy.cs \
MonoDevelop.Core.Gui.Codons/CommandCodon.cs \
MonoDevelop.Core.Gui.Codons/CommandItemCodon.cs \
MonoDevelop.Core.Gui.Codons/ItemSetCodon.cs \
MonoDevelop.Core.Gui.Codons/LinkItemCodon.cs \
MonoDevelop.Core.Gui.Codons/SeparatorItemCodon.cs \
MonoDevelop.Core.Gui.Components/MenuButtonEntry.cs \
MonoDevelop.Core.Gui.Codons/LocalCommandItemCodon.cs \
MonoDevelop.Core.Gui.ProgressMonitoring/MessageDialogProgressMonitor.cs \
MonoDevelop.Core.Gui.Codons/StockIconCodon.cs

RES= \
Base.glade \
icons/BreakPoint.png \
icons/ExecutionMarker.png \
icons/FileIcons.XmlIcon \
icons/GeneralWizardBackground \
icons/Icons.16x16.AboutIcon \
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
icons/Icons.16x16.CopyLeftIcon \
icons/Icons.16x16.DRIVE \
icons/Icons.16x16.Delegate \
icons/Icons.16x16.Desktop \
icons/Icons.16x16.Empty \
icons/Icons.16x16.Enum \
icons/Icons.16x16.Event \
icons/Icons.16x16.Field \
icons/Icons.16x16.FindIcon \
icons/Icons.16x16.FindInFiles \
icons/Icons.16x16.FindNextIcon \
icons/Icons.16x16.FullScreen \
icons/Icons.16x16.GotoNextbookmark \
icons/Icons.16x16.GotoPrevbookmark \
icons/Icons.16x16.Information \
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
icons/Icons.16x16.Library \
icons/Icons.16x16.Literal \
icons/Icons.16x16.Method \
icons/Icons.16x16.MiscFiles \
icons/Icons.16x16.MyComputer \
icons/Icons.16x16.NETWORK \
icons/Icons.16x16.NameSpace \
icons/Icons.16x16.NewFolderIcon \
icons/Icons.16x16.NewSolutionIcon.png \
icons/Icons.16x16.NewCombineIcon.png \
icons/Icons.16x16.NextWindowIcon \
icons/Icons.16x16.OpenFileIcon \
icons/Icons.16x16.OpenFolderBitmap \
icons/Icons.16x16.OpenProjectIcon \
icons/Icons.16x16.OpenReferenceFolder \
icons/Icons.16x16.OpenResourceFolder \
icons/Icons.16x16.OutputIcon \
icons/Icons.16x16.PersonalFiles \
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
icons/Icons.16x16.SelectionArrow \
icons/Icons.16x16.SolutionIcon \
icons/Icons.16x16.SplitWindow \
icons/Icons.16x16.Struct \
icons/Icons.16x16.SubTypes \
icons/Icons.16x16.SuperTypes \
icons/Icons.16x16.TaskListIcon \
icons/Icons.16x16.TextFileIcon \
icons/Icons.16x16.TipOfTheDay \
icons/Icons.16x16.ToggleBookmark \
icons/Icons.16x16.WebSearchIcon \
icons/Icons.16x16.XMLFileIcon \
icons/Icons.32x32.ASPFileIcon \
icons/Icons.32x32.CombineIcon \
icons/Icons.32x32.EmptyFileIcon \
icons/Icons.32x32.EmptyProjectIcon \
icons/Icons.32x32.HTMLFileIcon \
icons/Icons.32x32.ResourceFileIcon \
icons/Icons.32x32.TextFileIcon \
icons/Icons.32x32.XMLFileIcon \
icons/Icons.AboutImage \
icons/Icons.SharpDevelopIcon \
icons/Icons.TipOfTheDayIcon \
icons/package-x-generic_22.png \
icons/package-x-generic.png \
icons/system-software-update.png \
icons/system-software-update_22.png \
icons/gnome-fs-regular.png \
icons/user-package.png \
icons/comment.png \
icons/uncomment.png \
icons/visual-studio.png

GENERATED_FILES = \
AssemblyInfo.cs

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

ADDIN = MonoDevelop.Core.Gui.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(RES)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(RES:%=/resource:%) $(build_sources)

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES) $(ADDIN)

include $(top_srcdir)/Makefile.include
