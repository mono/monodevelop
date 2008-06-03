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
using Mono.Addins;
using MonoDevelop.Projects.Text;
using Gtk;

#pragma warning disable 612

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public class FileSelectorDialog: FileSelector
	{
		Hashtable filterPairs;
		int selectOption;
		int firstEncIndex;
		
		Gtk.Label encodingLabel;
		Gtk.OptionMenu encodingMenu;
		Gtk.Label viewerLabel;
		Gtk.ComboBox viewerSelector;
		Gtk.CheckButton closeWorkspaceCheck;
		ArrayList currentViewers = new ArrayList ();
		
		public FileSelectorDialog (string title): this (title, Gtk.FileChooserAction.Open)
		{
		}
		
		public FileSelectorDialog (string title, Gtk.FileChooserAction action): base (title, action)
		{
			LocalOnly = true;
			
			ArrayList filters = new ArrayList ();
			filters.AddRange (AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/ProjectFileFilters"));
			try
			{
				filters.AddRange (AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/FileFilters"));
			}
			catch
			{
				//nothing there..	
			}
			
			filterPairs = new Hashtable ();
			foreach (string filterStr in filters)
			{
				string[] parts = filterStr.Split ('|');
				Gtk.FileFilter filter = new Gtk.FileFilter ();
				filter.Name = parts[0];
				filterPairs[parts[0]] = parts[1];
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
			filterPairs[GettextCatalog.GetString ("All Files")] = ("*");
			AddFilter (allFilter);
			
			// Load last used filter
			string lastPattern = (string)PropertyService.Get ("Monodevelop.FileSelector.LastPattern", "*");
			foreach (FileFilter filter in this.Filters)
			{
				string pattern = filterPairs[filter.Name] as string;
				if (! String.IsNullOrEmpty (pattern) && pattern == lastPattern)
				{
					this.Filter = filter;
					break;
				}
			}
			
			// Add the text encoding selector
			Table table = new Table (2, 2, false);
			table.RowSpacing = 6;
			table.ColumnSpacing = 6;
			
			encodingLabel = new Label (GettextCatalog.GetString ("_Character Coding:"));
			encodingLabel.Xalign = 0;
			table.Attach (encodingLabel, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			
			encodingMenu = new Gtk.OptionMenu ();
			FillEncodings ();
			encodingMenu.SetHistory (0);
			table.Attach (encodingMenu, 1, 2, 0, 1, AttachOptions.Expand|AttachOptions.Fill, AttachOptions.Expand|AttachOptions.Fill, 0, 0);

			encodingMenu.Changed += EncodingChanged;
			
			// Add the viewer selector
			viewerLabel = new Label (GettextCatalog.GetString ("Open With:"));
			viewerLabel.Xalign = 0;
			table.Attach (viewerLabel, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			
			Gtk.HBox box = new HBox (false, 6);
			viewerSelector = Gtk.ComboBox.NewText ();
			box.PackStart (viewerSelector, true, true, 0);
			closeWorkspaceCheck = new CheckButton (GettextCatalog.GetString ("Close current workspace"));
			closeWorkspaceCheck.Active = true;
			box.PackStart (closeWorkspaceCheck, false, false, 0);
			table.Attach (box, 1, 2, 1, 2, AttachOptions.Expand|AttachOptions.Fill, AttachOptions.Expand|AttachOptions.Fill, 0, 0);
			FillViewers ();
			viewerSelector.Changed += OnViewerChanged;
			
			table.ShowAll ();
			this.ExtraWidget = table;
			
			// Give back the height that the extra widgets take
			int w, h;
			GetSize (out w, out h);
			Resize (w, h + table.SizeRequest ().Height);
			
			if (action == Gtk.FileChooserAction.SelectFolder)
				ShowEncodingSelector = false;
				
			if (action != Gtk.FileChooserAction.Open)
				ShowViewerSelector = false;
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
			get { return encodingLabel.Visible; }
			set { encodingLabel.Visible = encodingMenu.Visible = value; }
		}
		
		public bool ShowViewerSelector {
			get { return viewerLabel.Visible; }
			set { viewerLabel.Visible = viewerSelector.Visible = value; }
		}
		
		public bool CloseCurrentWorkspace {
			get { return closeWorkspaceCheck.Active; }
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
				SelectEncodingsDialog dlg = new SelectEncodingsDialog ();
				dlg.TransientFor = this;
				dlg.Run ();
				dlg.Destroy ();
				FillEncodings ();
			}
		}
		
		void FillViewers ()
		{
			((Gtk.ListStore)viewerSelector.Model).Clear ();
			currentViewers.Clear ();
			
			if (Filename == null || Filename.Length == 0 || System.IO.Directory.Exists (Filename))
				return;
			
			if (IdeApp.Services.ProjectService.IsWorkspaceItemFile (Filename) || IdeApp.Services.ProjectService.IsSolutionItemFile (Filename)) {
				viewerSelector.AppendText (GettextCatalog.GetString ("Solution Workbench"));
				currentViewers.Add (null);
			}
			FileViewer[] vs = IdeApp.Workbench.GetFileViewers (Filename);
			foreach (FileViewer vw in vs) {
				if (!vw.IsExternal) {
					viewerSelector.AppendText (vw.Title);
					currentViewers.Add (vw);
				}
			}
			viewerSelector.Active = 0;
			viewerLabel.Sensitive = viewerSelector.Sensitive = currentViewers.Count > 1;
		}
		
		void OnViewerChanged (object s, EventArgs args)
		{
			UpdateExtraWidgets ();
		}
		
		void UpdateExtraWidgets ()
		{
			if (Filename == null || Filename.Length == 0 || System.IO.Directory.Exists (Filename)) {
				encodingLabel.Sensitive = encodingMenu.Sensitive = false;
				viewerLabel.Sensitive = viewerSelector.Sensitive = false;
				closeWorkspaceCheck.Visible = false;
				return;
			}
			   
			if (IdeApp.Services.ProjectService.IsWorkspaceItemFile (Filename) || IdeApp.Services.ProjectService.IsSolutionItemFile (Filename)) {
				encodingLabel.Sensitive = encodingMenu.Sensitive = (SelectedViewer != null);
				closeWorkspaceCheck.Visible = viewerLabel.Visible && IdeApp.Workspace.IsOpen;
			}
			else {
				encodingLabel.Sensitive = encodingMenu.Sensitive = true;
				closeWorkspaceCheck.Visible = false;
			}

			viewerLabel.Sensitive = viewerSelector.Sensitive = currentViewers.Count > 1;
		}
		
		public FileViewer SelectedViewer {
			get {
				if (viewerSelector.Active == -1)
					return null;
				return currentViewers [viewerSelector.Active] as FileViewer;
			}
		}
		
		protected override void OnSelectionChanged ()
		{
			base.OnSelectionChanged ();
			
			if (ExtraWidget == null || this.Action != Gtk.FileChooserAction.Open)
				return;
			
			UpdateExtraWidgets ();
			FillViewers ();
		}
		
		protected override void OnDestroyed ()
		{
			// Save active filter
			string pattern = filterPairs[this.Filter.Name] as string;
			if (pattern != null)
				PropertyService.Set ("Monodevelop.FileSelector.LastPattern", pattern);

			base.OnDestroyed ();
		}
	}
}

#pragma warning restore 612
