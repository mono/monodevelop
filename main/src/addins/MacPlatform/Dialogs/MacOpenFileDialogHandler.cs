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

using AppKit;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui;
using MonoDevelop.MacInterop;


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
				return new NSSavePanel ();
			}

			return new NSOpenPanel {
				CanChooseDirectories = (data.Action & FileChooserAction.FolderFlags) != 0,
				CanChooseFiles = (data.Action & FileChooserAction.FileFlags) != 0,
			};
		}

		public bool Run (OpenFileDialogData data)
		{
			try {
				using (var panel = CreatePanel (data, out var state)) {
					bool pathAlreadySet = false;
					panel.DidChangeToDirectory += (sender, e) => {
						var directoryPath = e.NewDirectoryUrl?.AbsoluteString;
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
						System.Threading.Tasks.Task.Delay (1).ContinueWith (delegate { panel.Cancel (panel); }, Runtime.MainTaskScheduler);
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

					if (panel.RunModal () == 0 && !pathAlreadySet) {
						GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
						return false;
					}
					if (!pathAlreadySet)
						data.SelectedFiles = MacSelectFileDialogHandler.GetSelectedFiles (panel);

					if (state.EncodingSelector != null)
						data.Encoding = state.EncodingSelector.SelectedEncodingId > 0 ? Encoding.GetEncoding (state.EncodingSelector.SelectedEncodingId) : null;

					if (state.ViewerSelector != null) {
						if (state.CloseSolutionButton != null)
							data.CloseCurrentWorkspace = state.CloseSolutionButton.State != NSCellStateValue.Off;
						data.SelectedViewer = state.ViewerSelector.IndexOfSelectedItem >= 0 ?
							state.CurrentViewers [(int)state.ViewerSelector.IndexOfSelectedItem] : null;
					}

					GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
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
				encodingSelector = new SelectEncodingPopUpButton (data.Action != FileChooserAction.Save) {
					SelectedEncodingId = data.Encoding != null ? data.Encoding.CodePage : 0
				};

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

			if (IdeApp.Services.ProjectService.IsWorkspaceItemFile (filename) || IdeApp.Services.ProjectService.IsSolutionItemFile (filename)) {
				button.Menu.AddItem (new NSMenuItem { Title = GettextCatalog.GetString ("Solution Workbench") });
				currentViewers.Add (null);
				
				if (closeSolutionButton != null)
					closeSolutionButton.State = NSCellStateValue.On;
				
				if (!CanBeOpenedInAssemblyBrowser (filename))
					selected = 0;
				hasWorkbenchViewer = true;
				i++;
			}
			
			foreach (var vw in DisplayBindingService.GetFileViewers (filename, null)) {
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
