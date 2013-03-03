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
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using MonoMac.Foundation;
using MonoMac.AppKit;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Components.Extensions;
using MonoDevelop.MacInterop;
using MonoDevelop.Ide.Gui;
using System.Text;

namespace MonoDevelop.MacIntegration
{
	class MacOpenFileDialogHandler : IOpenFileDialogHandler
	{
		public bool Run (OpenFileDialogData data)
		{
			NSSavePanel panel = null;
			
			try {
				bool directoryMode = data.Action != Gtk.FileChooserAction.Open
						&& data.Action != Gtk.FileChooserAction.Save;
				
				if (data.Action == Gtk.FileChooserAction.Save) {
					panel = new NSSavePanel ();
				} else {
					panel = new NSOpenPanel () {
						CanChooseDirectories = directoryMode,
						CanChooseFiles = !directoryMode,
					};
				}
				
				MacSelectFileDialogHandler.SetCommonPanelProperties (data, panel);
				
				SelectEncodingPopUpButton encodingSelector = null;
				NSPopUpButton viewerSelector = null;
				NSButton closeSolutionButton = null;
				
				var box = new MDBox (LayoutDirection.Vertical, 2, 2);
				
				List<FileViewer> currentViewers = null;
				List<MDAlignment> labels = new List<MDAlignment> ();
				
				if (!directoryMode) {
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
						encodingSelector = new SelectEncodingPopUpButton (data.Action != Gtk.FileChooserAction.Save);
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
						viewerSelector = new NSPopUpButton () {
							Enabled = false,
						};
						
						if (encodingSelector != null) {
							viewerSelector.Activated += delegate {
								var idx = viewerSelector.IndexOfSelectedItem;
								encodingSelector.Enabled = ! (idx == 0 && currentViewers [0] == null);
							};
						}
						
						var viewSelLabel = new MDLabel (GettextCatalog.GetString ("Open with:"));
						var viewSelBox = new MDBox (LayoutDirection.Horizontal, 2, 0) {
							{ viewSelLabel, true },
							{ new MDAlignment (viewerSelector, true) { MinWidth = 200 }  }
						};
						
						if (IdeApp.Workspace.IsOpen) {
							closeSolutionButton = new NSButton () {
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
				
				panel.SelectionDidChange += delegate(object sender, EventArgs e) {
					var selection = MacSelectFileDialogHandler.GetSelectedFiles (panel);
					bool slnViewerSelected = false;
					if (viewerSelector != null) {
						FillViewers (currentViewers, viewerSelector, closeSolutionButton, selection);
						if (currentViewers.Count == 0 || currentViewers [0] != null) {
							if (closeSolutionButton != null)
								closeSolutionButton.Hidden = true;
							slnViewerSelected = false;
						} else {
							if (closeSolutionButton != null)
								closeSolutionButton.Hidden = false;
							slnViewerSelected = true;
						}
						box.Layout ();
						
						//re-center the accessory view in its parent, Cocoa does this for us initially and after
						//resizing the window, but we need to do it again after altering its layout
						var superFrame = box.View.Superview.Frame;
						var frame = box.View.Frame;
						//not sure why it's ceiling, but this matches the Cocoa layout
						frame.X = (float)Math.Ceiling ((superFrame.Width - frame.Width) / 2);
						frame.Y = (float)Math.Ceiling ((superFrame.Height - frame.Height) / 2);
						box.View.Frame = frame;
					} 
					if (encodingSelector != null)
						encodingSelector.Enabled = !slnViewerSelected;
				};
				
				try {
					var action = MacSelectFileDialogHandler.RunPanel (data, panel);
					if (!action) {
						GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
						return false;
					}
				} catch (Exception ex) {
					System.Console.WriteLine (ex);
					throw;
				}
				
				data.SelectedFiles = MacSelectFileDialogHandler.GetSelectedFiles (panel);
				
				if (encodingSelector != null)
					data.Encoding = encodingSelector.SelectedEncodingId > 0 ? Encoding.GetEncoding (encodingSelector.SelectedEncodingId) : null;
				
				if (viewerSelector != null ) {
					if (closeSolutionButton != null)
						data.CloseCurrentWorkspace = closeSolutionButton.State != NSCellStateValue.Off;
					data.SelectedViewer = currentViewers[viewerSelector.IndexOfSelectedItem];
				}
				
				GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
			} catch (Exception ex) {
				MessageService.ShowException (ex);
			} finally {
				if (panel != null)
					panel.Dispose ();
			}
			return true;
		}
		
		static void FillViewers (List<FileViewer> currentViewers, NSPopUpButton button, NSButton closeSolutionButton, FilePath[] filenames)
		{
			button.Menu.RemoveAllItems ();
			currentViewers.Clear ();
			
			if (filenames == null || filenames.Length == 0) {
				button.Enabled = false;
				return;
			}
			
			var filename = filenames[0];
			if (System.IO.Directory.Exists (filename))
				return;
			
			int selected = -1;
			int i = 0;
			
			if (IdeApp.Services.ProjectService.IsWorkspaceItemFile (filename) || IdeApp.Services.ProjectService.IsSolutionItemFile (filename)) {
				button.Menu.AddItem (new NSMenuItem () { Title = GettextCatalog.GetString ("Solution Workbench") });
				currentViewers.Add (null);
				
				if (closeSolutionButton != null)
					closeSolutionButton.State = NSCellStateValue.On;
				
				selected = 0;
				i++;
			}
			
			foreach (var vw in DisplayBindingService.GetFileViewers (filename, null)) {
				if (!vw.IsExternal) {
					button.Menu.AddItem (new NSMenuItem () { Title = vw.Title });
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
		}
	}
}
