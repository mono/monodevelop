//
// FileSelectorDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using MonoDevelop.Components;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Projects.Text;
using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public class FileSelectorDialog: FileSelector
	{
		Gtk.OptionMenu encodingMenu;
		int selectOption;
		int firstEncIndex;
		
		public FileSelectorDialog (string title): this (title, Gtk.FileChooserAction.Open)
		{
		}
		
		public FileSelectorDialog (string title, Gtk.FileChooserAction action): base (title, action)
		{
			ArrayList filters = new ArrayList ();
			filters.AddRange (AddInTreeSingleton.AddInTree.GetTreeNode ("/SharpDevelop/Workbench/Combine/FileFilter").BuildChildItems (this));
			try
			{
				filters.AddRange (AddInTreeSingleton.AddInTree.GetTreeNode ("/SharpDevelop/Workbench/FileFilter").BuildChildItems (this));
			}
			catch
			{
				//nothing there..	
			}
			
			foreach (string filterStr in filters)
			{
				string[] parts = filterStr.Split ('|');
				Gtk.FileFilter filter = new Gtk.FileFilter ();
				filter.Name = parts[0];
				foreach (string ext in parts[1].Split (';'))
				{
					filter.AddPattern (ext);
				}
				AddFilter (filter);
			}
			
			//Add All Files
			Gtk.FileFilter allFilter = new Gtk.FileFilter ();
			allFilter.Name = GettextCatalog.GetString ("All Files");
			allFilter.AddPattern ("*");
			AddFilter (allFilter);
			
			// Add the text encoding selector
			
			HBox box = new HBox ();
			Label lab = new Label (GettextCatalog.GetString ("_Character Coding:"));
			lab.Xalign = 0;
			box.PackStart (lab, false, false, 0);
			
			encodingMenu = new Gtk.OptionMenu ();
			FillEncodings ();
			encodingMenu.SetHistory (0);
			box.PackStart (encodingMenu, true, true, 6);
			box.ShowAll ();

			this.ExtraWidget = box;
			
			encodingMenu.Changed += EncodingChanged;
			
			if (action == Gtk.FileChooserAction.SelectFolder)
				ShowEncodingSelector = false;
		}
		
		public string Encoding {
			get {
				if (!ShowEncodingSelector)
					return null;
				else if (encodingMenu.History < firstEncIndex || encodingMenu.History == selectOption)
					return null;
				else
					return TextEncoding.ConversionEncodings [encodingMenu.History - firstEncIndex].Id;
			}
			set {
				for (uint n=0; n<TextEncoding.ConversionEncodings.Length; n++) {
					if (TextEncoding.ConversionEncodings [n].Id == value) {
						encodingMenu.SetHistory (n + (uint)firstEncIndex);
						Menu menu = (Menu) encodingMenu.Menu;
						RadioMenuItem rm = (RadioMenuItem) menu.Children [n + firstEncIndex];
						rm.Active = true;
						return;
					}
				}
				encodingMenu.SetHistory (0);
			}
		}
		
		public bool ShowEncodingSelector {
			get { return ExtraWidget.Visible; }
			set { ExtraWidget.Visible = value; }
		}
		
		void FillEncodings ()
		{
			selectOption = -1;
			RadioMenuItem defaultActivated = null;
			
			Gtk.Menu menu = new Menu ();
			
			// Don't show the auto-detection option when saving
			
			if (Action != Gtk.FileChooserAction.Save) {
				RadioMenuItem autodetect = new RadioMenuItem (GettextCatalog.GetString ("Auto Detected"));
				autodetect.Group = new GLib.SList (typeof(object));
				menu.Append (autodetect);
				menu.Append (new Gtk.SeparatorMenuItem ());
				autodetect.Active = true;
				defaultActivated = autodetect;
				firstEncIndex = 2;
			} else
				firstEncIndex = 0;
			
			foreach (TextEncoding e in TextEncoding.ConversionEncodings) {
				RadioMenuItem mitem = new RadioMenuItem (e.Name + " (" + e.Id + ")");
				menu.Append (mitem);
				if (defaultActivated == null) {
					defaultActivated = mitem;
					defaultActivated.Group = new GLib.SList (typeof(object));
				} else {
					mitem.Group = defaultActivated.Group;
				}
				mitem.Active = false;
			}
			
			if (defaultActivated != null)
				defaultActivated.Active = true;
			
			menu.Append (new Gtk.SeparatorMenuItem ());
			
			MenuItem select = new MenuItem (GettextCatalog.GetString ("Add or _Remove..."));
			menu.Append (select);
			
			menu.ShowAll ();
			encodingMenu.Menu = menu;
			
			encodingMenu.SetHistory (0);
					
			selectOption = firstEncIndex + TextEncoding.ConversionEncodings.Length + 1;
		}
		
		void EncodingChanged (object s, EventArgs args)
		{
			if (encodingMenu.History == selectOption) {
				using (SelectEncodingsDialog dlg = new SelectEncodingsDialog ()) {
					dlg.Run ();
					FillEncodings ();
				}
			}
		}
		
		protected override void OnSelectionChanged ()
		{
			base.OnSelectionChanged ();
			
			if (ExtraWidget == null || this.Action != Gtk.FileChooserAction.Open)
				return;
			
			if (Filename != null && Filename.Length > 0 && 
			    !MonoDevelop.Projects.Services.ProjectService.IsCombineEntryFile (Filename) &&
			    !System.IO.Directory.Exists (Filename))
			{
				ExtraWidget.Sensitive = true;
			} else
				ExtraWidget.Sensitive = false;
		}
	}
}
