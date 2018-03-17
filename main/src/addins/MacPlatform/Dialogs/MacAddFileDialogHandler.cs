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
using AppKit;
using CoreGraphics;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MacIntegration
{
	class MacAddFileDialogHandler : IAddFileDialogHandler
	{
		public bool Run (AddFileDialogData data)
		{
			using (var panel = new NSOpenPanel {
				CanChooseDirectories = false,
				CanChooseFiles = true,
			}) {
				MacSelectFileDialogHandler.SetCommonPanelProperties (data, panel);

				var labels = new List<MDAlignment> ();
				var controls = new List<MDAlignment> ();

				var accessoryBox = new MDAccessoryViewBox ();

				var filterPopup = MacSelectFileDialogHandler.CreateFileFilterPopup (data, panel);
				if (filterPopup != null) {
					accessoryBox.AddControl (filterPopup, GettextCatalog.GetString ("Show Files:"));
				}

				var popup = new NSPopUpButton (new CGRect (0, 0, 200, 28), false);
				popup.AddItem (GettextCatalog.GetString ("(Default)"));
				popup.Menu.AddItem (NSMenuItem.SeparatorItem);

				foreach (var b in data.BuildActions) {
					if (b == "--")
						popup.Menu.AddItem (NSMenuItem.SeparatorItem);
					else
						popup.AddItem (b);
				}

				accessoryBox.AddControl (popup, GettextCatalog.GetString ("Override build action:"));

				accessoryBox.Layout ();
				panel.AccessoryView = accessoryBox.View;

				if (panel.RunModal () == 0) {
					GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
					return false;
				}
				
				data.SelectedFiles = MacSelectFileDialogHandler.GetSelectedFiles (panel);
				
				var idx = popup.IndexOfSelectedItem - 2;
				if (idx >= 0)
					data.OverrideAction = data.BuildActions[idx];
				
				GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
				return true;
			}
		}
	}
}
