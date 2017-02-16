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
	class MacOpenFileDialogHandler : IOpenFileDialogHandler
	{
		public bool Run (OpenFileDialogData data)
		{
			NSSavePanel panel = null;
			
			try {
				if (data.Action == FileChooserAction.Save) {
					panel = new NSSavePanel ();
				} else {
					panel = new NSOpenPanel {
						CanChooseDirectories = (data.Action & FileChooserAction.FolderFlags) != 0,
						CanChooseFiles = (data.Action & FileChooserAction.FileFlags) != 0,
					};
				}

				MacSelectFileDialogHandler.SetCommonPanelProperties (data, panel);
				
				SelectEncodingPopUpButton encodingSelector = null;
				NSPopUpButton viewerSelector = null;
				NSButton closeSolutionButton = null;
				
				var box = new MDBox (LayoutDirection.Vertical, 2, 2);
				
				List<FileViewer> currentViewers = null;
				var labels = new List<MDAlignment> ();
				
				if ((data.Action & FileChooserAction.FileFlags) != 0) {
					var filterPopup = MacSelectFileDialogHandler.CreateFileFilterPopup (data, panel);

					if (filterPopup != null) {
						var filterLabel = new MDAlignment (new MDLabel (GettextCatalog.GetString ("Show files:")), true);
						var filterBox = new MDBox (LayoutDirection.Horizontal, 2, 0) {
							{ filterLabel },
							{ new MDAlignment (filterPopup, true) { MinWidth = 200 } }
						};
						labels.Add (filterLabel);
						box.Add (filterBox);
					}

					if (data.ShowEncodingSelector) {
						encodingSelector = new SelectEncodingPopUpButton (data.Action != FileChooserAction.Save);
						encodingSelector.SelectedEncodingId = data.Encoding != null ? data.Encoding.CodePage : 0;
						
						var encodingLabel = new MDAlignment (new MDLabel (GettextCatalog.GetString ("Encoding:")), true);
						var encodingBox = new MDBox (LayoutDirection.Horizontal, 2, 0) {
							{ encodingLabel },
							{ new MDAlignment (encodingSelector, true) { MinWidth = 200 }  }
						};
						labels.Add (encodingLabel);
						box.Add (encodingBox);
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
									if (closeSolutionButton.Hidden == workbenchViewerSelected) {
										closeSolutionButton.Hidden = !workbenchViewerSelected;
										CenterAccessoryView (box);
									}
								}
							};
						}
						
						var viewSelLabel = new MDLabel (GettextCatalog.GetString ("Open with:"));
						var viewSelBox = new MDBox (LayoutDirection.Horizontal, 2, 0) {
							{ viewSelLabel, true },
							{ new MDAlignment (viewerSelector, true) { MinWidth = 200 }  }
						};
						
						if (IdeApp.Workspace.IsOpen) {
							closeSolutionButton = new NSButton {
								Title = GettextCatalog.GetString ("Close current workspace"),
								Hidden = true,
								State = NSCellStateValue.On,
							};
							
							closeSolutionButton.SetButtonType (NSButtonType.Switch);
							closeSolutionButton.SizeToFit ();
							
							viewSelBox.Add (closeSolutionButton, true);
						}
						
						box.Add (viewSelBox);
					}
				}
				
				if (labels.Count > 0) {
					float w = labels.Max (l => l.MinWidth);
					foreach (var l in labels) {
						l.MinWidth = w;
						l.XAlign = LayoutAlign.Begin;
					}
				}
				
				if (box.Count > 0) {
					box.Layout ();
					panel.AccessoryView = box.View;
				}
				
				panel.SelectionDidChange += delegate {
					var selection = MacSelectFileDialogHandler.GetSelectedFiles (panel);
					bool slnViewerSelected = false;
					if (viewerSelector != null) {
						slnViewerSelected = FillViewers (currentViewers, viewerSelector, closeSolutionButton, selection);
						if (closeSolutionButton != null)
							closeSolutionButton.Hidden = !slnViewerSelected;
						CenterAccessoryView (box);
					} 
					if (encodingSelector != null)
						encodingSelector.Enabled = !slnViewerSelected;
				};

				if (panel.RunModal () == 0) {
					GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
					return false;
				}

				data.SelectedFiles = MacSelectFileDialogHandler.GetSelectedFiles (panel);
				
				if (encodingSelector != null)
					data.Encoding = encodingSelector.SelectedEncodingId > 0 ? Encoding.GetEncoding (encodingSelector.SelectedEncodingId) : null;
				
				if (viewerSelector != null ) {
					if (closeSolutionButton != null)
						data.CloseCurrentWorkspace = closeSolutionButton.State != NSCellStateValue.Off;
					data.SelectedViewer = viewerSelector.IndexOfSelectedItem >= 0 ?
						currentViewers[(int)viewerSelector.IndexOfSelectedItem] : null;
				}
				
				GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Error in Open File dialog", ex);
			} finally {
				if (panel != null)
					panel.Dispose ();
			}
			return true;
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

		static void CenterAccessoryView (MDBox box)
		{
			box.Layout ();

			//re-center the accessory view in its parent, Cocoa does this for us initially and after
			//resizing the window, but we need to do it again after altering its layout
			var superView = box.View.Superview;
			if (superView == null)
				return;
			
			var superFrame = superView.Frame;
			var frame = box.View.Frame;
			//not sure why it's ceiling, but this matches the Cocoa layout
			frame.X = (float)Math.Ceiling ((superFrame.Width - frame.Width) / 2);
			frame.Y = (float)Math.Ceiling ((superFrame.Height - frame.Height) / 2);
			box.View.Frame = frame;
		}
	}
}
