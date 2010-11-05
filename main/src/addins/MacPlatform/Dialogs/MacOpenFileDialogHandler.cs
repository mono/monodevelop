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
using MonoDevelop.Components.Extensions;
using OSXIntegration.Framework;
using MonoDevelop.Ide.Extensions;
using MonoMac.AppKit;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoMac.Foundation;
using System.Linq;
using System.Drawing;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Runtime.InteropServices;

namespace MonoDevelop.Platform.Mac
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
					
					var filterLabel = new MDAlignment (new MDLabel (GettextCatalog.GetString ("Show files:")), true);
					var filterBox = new MDBox (LayoutDirection.Horizontal, 2, 0) {
						{ filterLabel },
						{ new MDAlignment (filterPopup, true) { MinWidth = 200 } }
					};
					labels.Add (filterLabel);
					box.Add (filterBox);
					
					if (data.ShowEncodingSelector) {
						encodingSelector = new SelectEncodingPopUpButton (data.Action != Gtk.FileChooserAction.Save);
						encodingSelector.SelectedEncodingId = data.Encoding;
						
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
								encodingSelector.Enabled = ! (idx == 0 && currentViewers[0] == null);
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
								State = 1,
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
					box.Layout (box.View.Superview.Frame.Size);
				}
				
				panel.SelectionDidChange += delegate(object sender, EventArgs e) {
					var selection = MacSelectFileDialogHandler.GetSelectedFiles (panel);
					bool slnViewerSelected = false;
					if (viewerSelector != null) {
						FillViewers (currentViewers, viewerSelector, selection);
						if (currentViewers.Count == 0 || currentViewers[0] != null) {
							if (closeSolutionButton != null)
								closeSolutionButton.Hidden = true;
							slnViewerSelected = false;
						} else {
							if (closeSolutionButton != null)
								closeSolutionButton.Hidden = false;
							slnViewerSelected = true;
						}
						box.Layout (box.View.Superview.Frame.Size);
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
					data.Encoding = encodingSelector.SelectedEncodingId;
				
				if (viewerSelector != null ) {
					if (closeSolutionButton != null)
						data.CloseCurrentWorkspace = closeSolutionButton.State != 0;
					data.SelectedViewer = currentViewers[viewerSelector.IndexOfSelectedItem];
				}
				
				GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
				return true;
			} finally {
				if (panel != null)
					panel.Dispose ();
			}
		}
		
		static void FillViewers (List<FileViewer> currentViewers, NSPopUpButton button, FilePath[] filenames)
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
			
			if (IdeApp.Services.ProjectService.IsWorkspaceItemFile (filename) || IdeApp.Services.ProjectService.IsSolutionItemFile (filename)) {
				button.Menu.AddItem (new NSMenuItem () { Title = GettextCatalog.GetString ("Solution Workbench") });
				currentViewers.Add (null);
			}
			foreach (var vw in IdeApp.Workbench.GetFileViewers (filename)) {
				if (!vw.IsExternal) {
					button.Menu.AddItem (new NSMenuItem () { Title = vw.Title });
					currentViewers.Add (vw);
				}
			}
			button.Enabled = currentViewers.Count > 1;
			button.SelectItem (0);
		}
	}
	
	static class GtkQuartz
	{
		//this may be needed to work around focusing issues in GTK/Cocoa interop
		public static void FocusWindow (Gtk.Window widget)
		{
			var window = GetWindow (widget);
			if (window != null)
				window.MakeKeyAndOrderFront (window);
		}
		
		public static NSWindow GetWindow (Gtk.Window window)
		{
			var ptr = gdk_quartz_window_get_nswindow (window.GdkWindow.Handle);
			if (ptr == IntPtr.Zero)
				return null;
			return MonoMac.ObjCRuntime.Runtime.GetNSObject (ptr) as NSWindow;
		}
		
		public static NSView GetView (Gtk.Widget widget)
		{
			var ptr = gdk_quartz_window_get_nsview (widget.GdkWindow.Handle);
			if (ptr == IntPtr.Zero)
				return null;
			return MonoMac.ObjCRuntime.Runtime.GetNSObject (ptr) as NSView;
		}
		
		[DllImport ("libgtk-quartz-2.0.dylib")]
        static extern IntPtr gdk_quartz_window_get_nsview (IntPtr window);
		
        [DllImport ("libgtk-quartz-2.0.dylib")]
        static extern IntPtr gdk_quartz_window_get_nswindow (IntPtr window);
	}
}
