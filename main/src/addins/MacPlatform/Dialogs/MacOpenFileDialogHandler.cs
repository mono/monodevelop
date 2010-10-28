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

namespace MonoDevelop.Platform.Mac
{
	class MacOpenFileDialogHandler : IOpenFileDialogHandler
	{
		public bool Run (OpenFileDialogData data)
		{
			NSSavePanel panel = null;
			
			try {
				bool directoryMode = data.Action != Gtk.FileChooserAction.Open;
				
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
				
				var box = new MDBox (MDBoxDirection.Vertical, 2);
				
				if (!directoryMode) {
					var filterPopup = MacSelectFileDialogHandler.CreateFileFilterPopup (data, panel);
					box.Add (filterPopup);
					
					if (data.ShowEncodingSelector) {
						encodingSelector = new SelectEncodingPopUpButton (data.Action != Gtk.FileChooserAction.Save);
						encodingSelector.SelectedEncodingId = data.Encoding;
						box.Add (MacSelectFileDialogHandler.LabelPopUp (
							GettextCatalog.GetString ("Encoding:"), 200, encodingSelector));
					}
				}
				
				if (box.Count > 0)
					panel.AccessoryView = box.CreateView ();
				
				var action = panel.RunModal ();
				if (action == 0)
					return false;
				
				data.SelectedFiles = MacSelectFileDialogHandler.GetSelectedFiles (panel);
				if (encodingSelector != null)
					data.Encoding = encodingSelector.SelectedEncodingId;
				
				return true;
			} finally {
				if (panel != null)
					panel.Dispose ();
			}
		}
	}
}
