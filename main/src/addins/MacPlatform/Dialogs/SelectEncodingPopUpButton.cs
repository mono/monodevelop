// 
// MacOpenFileDialogHandler.cs
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
using System.Linq;
using Foundation;
using AppKit;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.MacIntegration
{
	class SelectEncodingPopUpButton : NSPopUpButton
	{
		ObjCRuntime.Selector itemActivationSel = new ObjCRuntime.Selector ("itemActivated:");
		ObjCRuntime.Selector addRemoveActivationSel = new ObjCRuntime.Selector ("addRemoveActivated:");
		
		NSMenuItem autoDetectedItem, addRemoveItem;
		int[] encodings;
		
		public SelectEncodingPopUpButton (bool showAutoDetected)
		{
			Cell.UsesItemFromMenu = false;
			
			if (showAutoDetected) {
				autoDetectedItem = new NSMenuItem {
					Title = GettextCatalog.GetString ("Auto Detected"),
					Tag = -1,
					Target = this,
					Action = itemActivationSel,
				};
			}
			
			addRemoveItem = new NSMenuItem {
				Title = GettextCatalog.GetString ("Add or Remove..."),
				Tag = -20,
				Target = this,
				Action = addRemoveActivationSel,
			};
			
			Populate (false);
			SelectedEncodingId = 0;
		}
		
		public int SelectedEncodingId {
			get {
				var idx = Cell.MenuItem.Tag;
				if (idx <= 0)
					return 0;
				return encodings[idx - 1];
			}
			set {
				NSMenuItem item = null;
				if (value > 0) {
					int i = 1;
					foreach (var e in encodings) {
						if (e == value) {
							item = Menu.ItemWithTag (i);
							break;
						}
						i++;
					}
				}
				Cell.SelectItem (Cell.MenuItem = item ?? Cell.Menu.ItemAt (0));
			}
		}
		
		void Populate (bool clear)
		{
			if (clear)
				Menu.RemoveAllItems ();
				
			encodings = TextEncoding.ConversionEncodings.Select ((e) => e.CodePage).ToArray ();
			
			if (autoDetectedItem != null) {
				Menu.AddItem (autoDetectedItem);
				Cell.MenuItem = autoDetectedItem;
				Menu.AddItem (NSMenuItem.SeparatorItem);
			}
			
			int i = 1;
			foreach (var e in TextEncoding.ConversionEncodings) {
				Menu.AddItem (new NSMenuItem {
					Title = string.Format ("{0} ({1})", e.Name, e.Id),
					Tag = i++,
					Target = this,
					Action = itemActivationSel,
				});
			}
			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			Menu.AddItem (addRemoveItem);
		}
		
		[Export ("addRemoveActivated:")]
		void HandleAddRemoveItemActivated (NSObject sender)
		{
			Cell.SelectItem (Cell.MenuItem);
			
			var selection = SelectedEncodingId;
			var dlg = new SelectEncodingPanel ();
			if (dlg.RunModalSheet (Window) != 0) {
				Populate (true);
				SelectedEncodingId = selection;
			}
		}
		
		[Export ("itemActivated:")]
		void HandleItemActivated (NSObject sender)
		{
			Cell.MenuItem = (NSMenuItem)sender;
		}
	}
}
