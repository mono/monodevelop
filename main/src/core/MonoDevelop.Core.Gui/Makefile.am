
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_GUI_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONO_ADDINS_SETUP_LIBS) \
	-r:ICSharpCode.SharpZipLib \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Core.Gui.Dialogs.MultiMessageDialog.cs \
	gtk-gui/MonoDevelop.Core.Gui.Dialogs.MultiTaskProgressDialog.cs \
	gtk-gui/MonoDevelop.Core.Gui.Dialogs.OptionsDialog.cs \
	MonoDevelop.Core.Gui.Codons/DialogPanelCodon.cs \
	MonoDevelop.Core.Gui.Codons/MimeTypeNode.cs \
	MonoDevelop.Core.Gui.Codons/OptionsDialogSection.cs \
	MonoDevelop.Core.Gui.Codons/OptionsPanelNode.cs \
	MonoDevelop.Core.Gui.Codons/StockIconCodon.cs \
	MonoDevelop.Core.Gui.Components/FileBrowser.cs \
	MonoDevelop.Core.Gui.Components/MenuButtonEntry.cs \
	MonoDevelop.Core.Gui.Dialogs/AbstractOptionPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/DefaultDialogPanelDescriptor.cs \
	MonoDevelop.Core.Gui.Dialogs/ErrorDialog.cs \
	MonoDevelop.Core.Gui.Dialogs/IDialogPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/IDialogPanelDescriptor.cs \
	MonoDevelop.Core.Gui.Dialogs/IOptionsPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/MultiMessageDialog.cs \
	MonoDevelop.Core.Gui.Dialogs/MultiTaskProgressDialog.cs \
	MonoDevelop.Core.Gui.Dialogs/OptionsDialog.cs \
	MonoDevelop.Core.Gui.Dialogs/OptionsPanel.cs \
	MonoDevelop.Core.Gui.Dialogs/ProgressDialog.cs \
	MonoDevelop.Core.Gui.Dialogs/SetupApp.cs \
	MonoDevelop.Core.Gui.Dialogs/TreeViewOptions.cs \
	MonoDevelop.Core.Gui.ProgressMonitoring/BaseProgressMonitor.cs \
	MonoDevelop.Core.Gui.ProgressMonitoring/MessageDialogProgressMonitor.cs \
	MonoDevelop.Core.Gui.ProgressMonitoring/MultiTaskDialogProgressMonitor.cs \
	MonoDevelop.Core.Gui.WebBrowser/IWebBrowser.cs \
	MonoDevelop.Core.Gui.WebBrowser/IWebBrowserLoader.cs \
	MonoDevelop.Core.Gui.WebBrowser/LoadingProgressChangedEventArgs.cs \
	MonoDevelop.Core.Gui.WebBrowser/LocationChangedEventArgs.cs \
	MonoDevelop.Core.Gui.WebBrowser/LocationChangingEventArgs.cs \
	MonoDevelop.Core.Gui.WebBrowser/StatusMessageChangedEventArgs.cs \
	MonoDevelop.Core.Gui.WebBrowser/TitleChangedEventArgs.cs \
	MonoDevelop.Core.Gui/AlertDialog.cs \
	MonoDevelop.Core.Gui/AsyncDispatchAttribute.cs \
	MonoDevelop.Core.Gui/DefaultPlatformService.cs \
	MonoDevelop.Core.Gui/DesktopApplication.cs \
	MonoDevelop.Core.Gui/DispatchService.cs \
	MonoDevelop.Core.Gui/FreeDispatchAttribute.cs \
	MonoDevelop.Core.Gui/GLibLogging.cs \
	MonoDevelop.Core.Gui/GuiService.cs \
	MonoDevelop.Core.Gui/GuiSyncContext.cs \
	MonoDevelop.Core.Gui/GuiSyncObject.cs \
	MonoDevelop.Core.Gui/IMementoCapable.cs \
	MonoDevelop.Core.Gui/MessageService.cs \
	MonoDevelop.Core.Gui/PixbufList.cs \
	MonoDevelop.Core.Gui/PlatformService.cs \
	MonoDevelop.Core.Gui/RecentFileStorage.cs \
	MonoDevelop.Core.Gui/RecentItem.cs \
	MonoDevelop.Core.Gui/RecentOpen.cs \
	MonoDevelop.Core.Gui/ResourceService.cs \
	MonoDevelop.Core.Gui/StockIcons.cs \
	MonoDevelop.Core.Gui/SyncContext.cs \
	MonoDevelop.Core.Gui/SyncContextAttribute.cs \
	MonoDevelop.Core.Gui/SyncObject.cs \
	MonoDevelop.Core.Gui/WebBrowserService.cs

RES =  \
	Base.glade \
	gtk-gui/gui.stetic \
	icons/breakpoint-16.png \
	icons/breakpoint-22.png \
	icons/breakpoint-disable-all-16.png \
	icons/breakpoint-disable-all-22.png \
	icons/breakpoint-disabled-16.png \
	icons/breakpoint-disabled-22.png \
	icons/breakpoint-invalid-16.png \
	icons/breakpoint-invalid-22.png \
	icons/breakpoint-new-16.png \
	icons/breakpoint-new-22.png \
	icons/breakpoint-on-off-16.png \
	icons/breakpoint-on-off-22.png \
	icons/breakpoint-remove-all-16.png \
	icons/breakpoint-remove-all-22.png \
	icons/build-project-16.png \
	icons/build-project-22.png \
	icons/build-solution-16.png \
	icons/build-solution-22.png \
	icons/comment.png \
	icons/edit-select-all.png \
	icons/edit-select-all_22.png \
	icons/element-class-16.png \
	icons/element-delegate-16.png \
	icons/element-enumeration-16.png \
	icons/element-event-16.png \
	icons/element-field-16.png \
	icons/element-interface-16.png \
	icons/element-literal-16.png \
	icons/element-method-16.png \
	icons/element-namespace-16.png \
	icons/element-property-16.png \
	icons/element-structure-16.png \
	icons/empty-category-16.png \
	icons/empty-category-22.png \
	icons/ExecutionMarker.png \
	icons/file-addin-16.png \
	icons/file-class-32.png \
	icons/file-enum-32.png \
	icons/file-interface-32.png \
	icons/file-struct-32.png \
	icons/file-xml-16.png \
	icons/file-xml-32.png \
	icons/GeneralWizardBackground \
	icons/gnome-fs-regular.png \
	icons/Icons.16x16.ClearAllBookmarks \
	icons/Icons.16x16.CloseAllDocuments \
	icons/Icons.16x16.CloseCombineIcon \
	icons/Icons.16x16.ClosedFolderBitmap \
	icons/Icons.16x16.ClosedReferenceFolder \
	icons/Icons.16x16.ClosedResourceFolder \
	icons/Icons.16x16.CombineIcon \
	icons/Icons.16x16.Empty \
	icons/Icons.16x16.FindInFiles \
	icons/Icons.16x16.FindNextIcon \
	icons/Icons.16x16.GotoNextbookmark \
	icons/Icons.16x16.GotoPrevbookmark \
	icons/Icons.16x16.MiscFiles \
	icons/Icons.16x16.NewCombineIcon.png \
	icons/Icons.16x16.NewFolderIcon \
	icons/Icons.16x16.NewSolutionIcon.png \
	icons/Icons.16x16.OpenFolderBitmap \
	icons/Icons.16x16.OpenProjectIcon \
	icons/Icons.16x16.OpenReferenceFolder \
	icons/Icons.16x16.OpenResourceFolder \
	icons/Icons.16x16.OutputIcon \
	icons/Icons.16x16.Reference \
	icons/Icons.16x16.ReplaceInFiles \
	icons/Icons.16x16.ResourceFileIcon \
	icons/Icons.16x16.SaveAllIcon \
	icons/Icons.16x16.SplitWindow \
	icons/Icons.16x16.TextFileIcon \
	icons/Icons.16x16.TipOfTheDay \
	icons/Icons.16x16.ToggleBookmark \
	icons/Icons.16x16.WebSearchIcon \
	icons/Icons.22x22.ClearAllBookmarks.png \
	icons/Icons.22x22.GotoNextbookmark.png \
	icons/Icons.22x22.GotoPrevbookmark.png \
	icons/Icons.22x22.ToggleBookmark.png \
	icons/Icons.24x24.SaveAllIcon \
	icons/Icons.32x32.EmptyFileIcon \
	icons/Icons.32x32.HTMLFileIcon \
	icons/Icons.32x32.ResourceFileIcon \
	icons/Icons.32x32.TextFileIcon \
	icons/Icons.32x32.XMLFileIcon \
	icons/Icons.SharpDevelopIcon \
	icons/Icons.SharpDevelopIcon-22.png \
	icons/keyword-16.png \
	icons/package-x-generic.png \
	icons/package-x-generic_22.png \
	icons/pad-task-list-16.png \
	icons/pad-task-list-24.png \
	icons/plugin-16.png \
	icons/plugin-22.png \
	icons/plugin-32.png \
	icons/project-16.png \
	icons/project-32.png \
	icons/project-console-32.png \
	icons/project-gui-32.png \
	icons/project-library-32.png \
	icons/solution-16.png \
	icons/solution-32.png \
	icons/solution-folder-closed-16.png \
	icons/solution-folder-open-16.png \
	icons/system-software-update.png \
	icons/system-software-update_22.png \
	icons/template-16.png \
	icons/uncomment.png \
	icons/user-package.png \
	icons/visibility-internal-16.png \
	icons/visibility-private-16.png \
	icons/visibility-protected-16.png \
	icons/visual-studio.png \
	icons/warning-overlay.png \
	icons/web-overlay-16.png \
	icons/web-overlay-32.png \
	icons/workspace-16.png \
	icons/workspace-32.png \
	MonoDevelop.Core.Gui.addin.xml

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
