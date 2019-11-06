// 
// MacSelectFileDialogHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AppKit;
using Foundation;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui;
using MonoDevelop.MacInterop;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.MacIntegration
{
	class MacOpenFileDialogHandler : MacCommonFileDialogHandler<OpenFileDialogData, MacOpenFileDialogHandler.SaveState>, IOpenFileDialogHandler
	{
		internal class SaveState
		{
			public SelectEncodingPopUpButton EncodingSelector { get; }
			public NSPopUpButton ViewerSelector { get; }
			public NSButton CloseSolutionButton { get; }
			public List<FileViewer> CurrentViewers { get; }

			public SaveState (SelectEncodingPopUpButton encodingSelector, NSPopUpButton viewerSelector, NSButton closeSolutionButton, List<FileViewer> currentViewers)
			{
				EncodingSelector = encodingSelector;
				ViewerSelector = viewerSelector;
				CloseSolutionButton = closeSolutionButton;
				CurrentViewers = currentViewers;
			}
		}

		protected override NSSavePanel OnCreatePanel (OpenFileDialogData data)
		{
			if (data.Action == FileChooserAction.Save) {
				return NSSavePanel.SavePanel;
			}

			var openPanel = NSOpenPanel.OpenPanel;
			openPanel.CanChooseDirectories = (data.Action & FileChooserAction.FolderFlags) != 0;
			openPanel.CanChooseFiles = (data.Action & FileChooserAction.FileFlags) != 0;
			return openPanel;
		}

		public bool Run (OpenFileDialogData data)
		{
			using var panelClosedSource = new CancellationTokenSource ();
			try {
				using (var panel = CreatePanel (data, out var state)) {
					bool pathAlreadySet = false;
					var panelClosedToken = panelClosedSource.Token;
					panel.DidChangeToDirectory += (sender, e) => {
						// HACK: On Catalina e.NewDirectoryUrl might be NSNull instead of null
						if (e.NewDirectoryUrl == null || ((NSObject)e.NewDirectoryUrl) is NSNull)
							return;
						var directoryPath = e.NewDirectoryUrl.AbsoluteString;
						if (string.IsNullOrEmpty (directoryPath))
							return;
						var selectedPath = data.OnDirectoryChanged (this, directoryPath);
						if (selectedPath.IsNull)
							return;
						data.SelectedFiles = new FilePath [] { selectedPath };
						pathAlreadySet = true;

						// We need to call Cancel on 1ms delay so it's executed after DidChangeToDirectory event handler is finished
						// this is needed because it's possible that DidChangeToDirectory event is executed while dialog is opening
						// in that case calling .Cancel() leaves dialog in weird state...
						// Fun fact: DidChangeToDirectory event is called from Open on 10.12 but not on 10.13
						System.Threading.Tasks.Task.Delay (1).ContinueWith (delegate {
							if (!panelClosedToken.IsCancellationRequested)
								panel.Cancel (panel);
						}, panelClosedToken, System.Threading.Tasks.TaskContinuationOptions.None, Runtime.MainTaskScheduler);
					};

					panel.SelectionDidChange += delegate {
						var selection = MacSelectFileDialogHandler.GetSelectedFiles (panel);
						bool slnViewerSelected = false;
						if (state.ViewerSelector != null) {
							slnViewerSelected = FillViewers (state.CurrentViewers, state.ViewerSelector, state.CloseSolutionButton, selection);
							if (state.CloseSolutionButton != null) {
								state.CloseSolutionButton.Enabled = slnViewerSelected;
								state.CloseSolutionButton.State = slnViewerSelected ? NSCellStateValue.On : NSCellStateValue.Off;
							}
						}
						if (state.EncodingSelector != null)
							state.EncodingSelector.Enabled = !slnViewerSelected;
					};

					var parent = data.TransientFor ?? MessageService.RootWindow;

					// TODO: support for data.CenterToParent, we could use sheeting.
					if (panel.RunModal () == 0 && !pathAlreadySet) {
						panelClosedSource.Cancel ();
						IdeServices.DesktopService.FocusWindow (parent);
						return false;
					}
					panelClosedSource.Cancel ();
					if (!pathAlreadySet)
						data.SelectedFiles = MacSelectFileDialogHandler.GetSelectedFiles (panel);

					if (state.EncodingSelector != null)
						data.Encoding = state.EncodingSelector.SelectedEncoding?.Encoding;

					if (state.ViewerSelector != null) {
						if (state.CloseSolutionButton != null)
							data.CloseCurrentWorkspace = state.CloseSolutionButton.State != NSCellStateValue.Off;
						data.SelectedViewer = state.ViewerSelector.IndexOfSelectedItem >= 0 ?
							state.CurrentViewers [(int)state.ViewerSelector.IndexOfSelectedItem] : null;
					}

					IdeServices.DesktopService.FocusWindow (parent);
				}
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Error in Open File dialog", ex);
			}
			return true;
		}

		protected override IEnumerable<(NSControl control, string text)> OnGetAccessoryBoxControls (OpenFileDialogData data, NSSavePanel panel, out SaveState saveState)
		{
			List<(NSControl, string)> controls = new List<(NSControl, string)> ();
			SelectEncodingPopUpButton encodingSelector = null;
			NSPopUpButton viewerSelector = null;
			NSButton closeSolutionButton = null;
			List<FileViewer> currentViewers = null;

			if (data.ShowEncodingSelector) {
				encodingSelector = new SelectEncodingPopUpButton (data.Action != FileChooserAction.Save);
				encodingSelector.SelectedEncoding = TextEncoding.GetEncoding (data.Encoding);

				controls.Add ((encodingSelector, GettextCatalog.GetString ("Encoding:")));
			}

			if (data.ShowViewerSelector && panel is NSOpenPanel) {
				currentViewers = new List<FileViewer> ();
				viewerSelector = new NSPopUpButton {
					Enabled = false,
				};

				if (encodingSelector != null || IdeApp.Workspace.IsOpen) {
					viewerSelector.Activated += delegate {
						var idx = viewerSelector.IndexOfSelectedItem;
						bool workbenchViewerSelected = idx == 0 && currentViewers [0] == null;
						if (encodingSelector != null)
							encodingSelector.Enabled = !workbenchViewerSelected;
						if (closeSolutionButton != null) {
							if (closeSolutionButton.Enabled != workbenchViewerSelected) {
								closeSolutionButton.Enabled = workbenchViewerSelected;
								closeSolutionButton.State = workbenchViewerSelected ? NSCellStateValue.On : NSCellStateValue.Off;
							}
						}
					};
				}

				if (IdeApp.Workspace.IsOpen) {
					closeSolutionButton = new NSButton {
						Title = GettextCatalog.GetString ("Close current workspace"),
						Enabled = false,
						State = NSCellStateValue.Off,
					};

					closeSolutionButton.SetButtonType (NSButtonType.Switch);
					closeSolutionButton.SizeToFit ();

					controls.Add ((closeSolutionButton, string.Empty));
				}

				controls.Add ((viewerSelector, GettextCatalog.GetString ("Open With:")));
			}
			saveState = new SaveState (encodingSelector, viewerSelector, closeSolutionButton, currentViewers);

			return controls;
		}

		static bool FillViewers (List<FileViewer> currentViewers, NSPopUpButton button, NSButton closeSolutionButton, FilePath[] filenames)
		{
			button.Menu.RemoveAllItems ();
			currentViewers.Clear ();
			
			if (filenames == null || filenames.Length == 0) {
				button.Enabled = false;
				return false;
			}
			
			var filename = filenames[0];
			if (System.IO.Directory.Exists (filename))
				return false;
			
			int selected = -1;
			int i = 0;
			bool hasWorkbenchViewer = false;

			if (IdeServices.ProjectService.IsWorkspaceItemFile (filename) || IdeServices.ProjectService.IsSolutionItemFile (filename)) {
				button.Menu.AddItem (new NSMenuItem { Title = GettextCatalog.GetString ("Solution Workbench") });
				currentViewers.Add (null);
				
				if (closeSolutionButton != null)
					closeSolutionButton.State = NSCellStateValue.On;
				
				if (!CanBeOpenedInAssemblyBrowser (filename))
					selected = 0;
				hasWorkbenchViewer = true;
				i++;
			}
			
			foreach (var vw in IdeServices.DisplayBindingService.GetFileViewers (filename, null).Result) {
				if (!vw.IsExternal) {
					button.Menu.AddItem (new NSMenuItem { Title = vw.Title });
					currentViewers.Add (vw);
					
					if (vw.CanUseAsDefault && selected == -1)
						selected = i;
					
					i++;
				}
			}
			
			if (selected == -1)
				selected = 0;
			
			button.Enabled = currentViewers.Count > 1;
			button.SelectItem (selected);
			return hasWorkbenchViewer;
		}

		static bool CanBeOpenedInAssemblyBrowser (FilePath filename)
		{
			return string.Equals (filename.Extension, ".exe", StringComparison.OrdinalIgnoreCase) || string.Equals (filename.Extension, ".dll", StringComparison.OrdinalIgnoreCase);
		}
	}
}
