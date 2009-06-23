
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment.Linux
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class DotDesktopViewWidget : Gtk.Bin
	{
		public event EventHandler Changed;
		
		DesktopEntry entry;

		ListStore storeEnvs;
		ListStore storeCategs;
		ListStore storeMimeTypes;
		ListStore storeEntries;
		int loading;
		
		CellRendererText entryKeyCell;
		CellRendererText mimeTypeCell;
		
		XmlDocument desktopInfo = DesktopEntry.GetDesktopInfo ();
		
		public DotDesktopViewWidget()
		{
			this.Build();
			notebook.Page = 0;
			
			// Environments tree
			
			storeEnvs = new ListStore (typeof(bool), typeof(string), typeof(string));
			treeEnvs.Model = storeEnvs;
			treeEnvs.HeadersVisible = false;
			
			TreeViewColumn col = new TreeViewColumn ();
			Gtk.CellRendererToggle tog = new CellRendererToggle ();
			col.PackStart (tog, false);
			tog.Toggled += OnEnvironmentClicked;
			
			Gtk.CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			
			col.AddAttribute (tog, "active", 0);
			col.AddAttribute (crt, "text", 1);
			treeEnvs.AppendColumn (col);
		
			// Selected categories tree
			
			storeCategs = new ListStore (typeof(string), typeof(string));
			treeCategories.Model = storeCategs;
			treeCategories.HeadersVisible = false;
			treeCategories.AppendColumn ("", new CellRendererText (), "text", 0);
			
			// Mime types
			
			storeMimeTypes = new ListStore (typeof(string), typeof(string));
			treeMimeTypes.Model = storeMimeTypes;
			mimeTypeCell = new CellRendererText ();
			mimeTypeCell.Edited += new Gtk.EditedHandler (HandleOnEditMimeType);
			mimeTypeCell.EditingCanceled += new EventHandler (HandleOnEditMimeTypeCancelled);
			mimeTypeCell.Editable = true;
			treeMimeTypes.AppendColumn (Mono.Unix.Catalog.GetString ("Mime Type"), mimeTypeCell, "text", 0);
			treeMimeTypes.AppendColumn (Mono.Unix.Catalog.GetString ("Description"), new CellRendererText (), "text", 1);
			
			// Other entries
			
			storeEntries = new ListStore (typeof(string), typeof(string), typeof(string)); 
			treeEntries.Model = storeEntries;
			entryKeyCell = new CellRendererText ();
			entryKeyCell.Edited += new Gtk.EditedHandler (HandleOnEditKey);
			entryKeyCell.EditingCanceled += new EventHandler (HandleOnEditKeyCancelled);
			entryKeyCell.Editable = false;
			treeEntries.AppendColumn (Mono.Unix.Catalog.GetString ("Key"), entryKeyCell, "markup", 0);
			crt = new CellRendererText ();
			crt.Edited += new Gtk.EditedHandler (HandleOnEditValue);
			crt.Editable = true;
			treeEntries.AppendColumn (Mono.Unix.Catalog.GetString ("Value"), crt, "text", 2);
		}
		
		void NotifyChanged ()
		{
			if (loading == 0 && Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public DesktopEntry DesktopEntry {
			get { return entry; }
			set { entry = value; Fill (); }
		}
		
		void Fill ()
		{
			loading++;
			
			comboType.Active = (int) entry.Type;
			entryExec.Text = entry.Exec;
			entryTryExec.Text = entry.TryExec;
			entryPath.Text = entry.Path;
			entryUrl.Text = entry.Url;
			checkTerminal.Active = entry.Terminal;
			
			comboLocales.AppendText (Mono.Unix.Catalog.GetString ("<Default>"));
			comboLocales.Active = 0;
			
			foreach (string loc in entry.GetLocales ())
				comboLocales.AppendText (loc);
			
			// Environments list
			
			if (entry.OnlyShowIn.Count > 0) {
				radioOnlyShowIn.Active = true;
			} else if (entry.NotShowIn.Count > 0) {
				radioNotShowIn.Active = true;
			} else
				radioAlwaysShow.Active = true;
			
			FillEnvironments ();
				
			// Fill mime types
			
			ArrayList sortedmt = new ArrayList ();
			sortedmt.AddRange (entry.MimeTypes);
			sortedmt.Sort ();
			
			foreach (string mt in sortedmt) {
				string desc = DesktopService.GetMimeTypeDescription (mt);
				storeMimeTypes.AppendValues (mt, desc);
			}
			
			checkShowInMenu.Active = !entry.NoDisplay;
			
			foreach (string s in entry.GetUnknownEntries ()) {
				storeEntries.AppendValues ("<b>" + s + "</b>", s, entry.GetEntry (s));
			}
			
			FillNames ();
			FillCategs ();
			UpdateType ();
			UpdateShowInMenu ();
			
			loading--;
		}
		
		void UpdateType ()
		{
			tableCommand.Visible = entry.Type == DesktopEntryType.Application;
			boxUrl.Visible = entry.Type == DesktopEntryType.Link;
			tableMimeTypes.Visible = entry.Type == DesktopEntryType.Application;
			boxCategories.Visible = entry.Type == DesktopEntryType.Application;
		}
		
		void UpdateShowInMenu ()
		{
			foreach (Gtk.Widget w in boxMenu.Children) {
				if (w != checkShowInMenu)
					w.Sensitive = checkShowInMenu.Active;
			}
		}
		
		void FillEnvironments ()
		{
			StringCollection envCol = null;
			if (entry.OnlyShowIn.Count > 0) {
				envCol = entry.OnlyShowIn;
			} else if (entry.NotShowIn.Count > 0) {
				envCol = entry.NotShowIn;
			}
			
			treeEnvs.Sensitive = envCol != null;
			
			storeEnvs.Clear ();
			foreach (XmlElement elem in desktopInfo.DocumentElement.SelectNodes ("Environments/Environment")) {
				bool sel = envCol != null && envCol.Contains (elem.GetAttribute ("name"));
				storeEnvs.AppendValues (sel, elem.GetAttribute ("_label"), elem.GetAttribute ("name"));
			}
		}
		
		void FillNames ()
		{
			loading++;
			entryName.Text = entry.Name;
			entryGenericName.Text = entry.GenericName;
			entryComment.Text = entry.Comment;
			entryIcon.Text = entry.Icon;
			loading--;
		}
		
		void FillCategs ()
		{
			storeCategs.Clear ();
			XmlElement cats = desktopInfo.DocumentElement ["Categories"];
			foreach (string cat in entry.Categories) {
				XmlNode node = cats.SelectSingleNode ("Category[@name='" + cat + "']/@_label");
				string catName;
				if (node != null)
					catName = node.InnerText;
				else
					catName = cat;
				storeCategs.AppendValues (Mono.Unix.Catalog.GetString (catName), cat);
			}
		}
		
		void OnEnvironmentClicked (object s, Gtk.ToggledArgs args)
		{
			TreeIter iter;
			storeEnvs.GetIterFromString (out iter, args.Path);
			
			bool sel = (bool) storeEnvs.GetValue (iter, 0);
			string env = (string) storeEnvs.GetValue (iter, 2);
			StringCollection col = radioOnlyShowIn.Active ? entry.OnlyShowIn : entry.NotShowIn;
			if (sel)
				col.Remove (env);
			else
				col.Add (env);
			
			storeEnvs.SetValue (iter, 0, !sel);
			NotifyChanged ();
		}

		protected virtual void OnButtonAddCategoriesClicked(object sender, System.EventArgs e)
		{
			MenuCategorySelectorDialog dlg = new MenuCategorySelectorDialog ();
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				foreach (string s in dlg.Selection)
					entry.Categories.Add (s);
				FillCategs ();
				NotifyChanged ();
			}
			dlg.Destroy ();
		}

		protected virtual void OnButtonRemoveCategoryClicked(object sender, System.EventArgs e)
		{
			TreeIter iter;
			if (treeCategories.Selection.GetSelected (out iter)) {
				string cat = (string) storeCategs.GetValue (iter, 1);
				entry.Categories.Remove (cat);
				storeCategs.Remove (ref iter);
				if (!iter.Equals (TreeIter.Zero))
					treeCategories.Selection.SelectIter (iter);
				NotifyChanged ();
			}
		}

		protected virtual void OnRadioAlwaysShowClicked(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			
			entry.NotShowIn.Clear ();
			entry.OnlyShowIn.Clear ();
			NotifyChanged ();
			FillEnvironments ();
			treeEnvs.Sensitive = false;
		}

		protected virtual void OnRadioOnlyShowInClicked(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			
			foreach (string s in entry.NotShowIn)
				entry.OnlyShowIn.Add (s);
			entry.NotShowIn.Clear ();
			
			treeEnvs.Sensitive = true;
			NotifyChanged ();
		}

		protected virtual void OnRadioNotShowInClicked(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
					
			foreach (string s in entry.OnlyShowIn)
				entry.NotShowIn.Add (s);
			entry.OnlyShowIn.Clear ();
			
			treeEnvs.Sensitive = true;
			NotifyChanged ();
		}

		protected virtual void OnButtonAddMimeTypeClicked(object sender, System.EventArgs e)
		{
			TreeIter it = storeMimeTypes.AppendValues ("", "");
			treeMimeTypes.Selection.SelectIter (it);
			treeMimeTypes.SetCursor (storeMimeTypes.GetPath (it), treeMimeTypes.Columns [0], true);
		}

		
		void HandleOnEditMimeType (object o, Gtk.EditedArgs e)
		{
			Gtk.TreeIter iter;
			if (storeMimeTypes.GetIterFromString (out iter, e.Path)) {
				string mt = e.NewText;
				string oldmt = (string) storeMimeTypes.GetValue (iter, 0);
				if (mt.Length > 0) {
					if (!entry.MimeTypes.Contains (mt)) {
						entry.MimeTypes.Add (mt);
						storeMimeTypes.SetValue (iter, 0, mt);
						storeMimeTypes.SetValue (iter, 1, DesktopService.GetMimeTypeDescription (mt));
						
						if (!string.IsNullOrEmpty (oldmt))
							// It is a modification. Remove the old name.
							entry.MimeTypes.Remove (oldmt);
						else {
							// Add a new line, so the user can add several types at a time
							TreeIter newit = storeMimeTypes.AppendValues ("", "");
							treeMimeTypes.Selection.SelectIter (newit);
							treeMimeTypes.ScrollToCell (storeMimeTypes.GetPath (newit), treeMimeTypes.Columns [0], false, 0f, 0f);
							treeMimeTypes.SetCursor (storeMimeTypes.GetPath (newit), treeMimeTypes.Columns [0], true);
							NotifyChanged ();
						}
					}
				}
				else {
					storeMimeTypes.Remove (ref iter);
					if (!string.IsNullOrEmpty (oldmt))
						entry.MimeTypes.Remove (oldmt);
				}
			}
		}
		
		void HandleOnEditMimeTypeCancelled (object s, EventArgs args)
		{
			entryKeyCell.Editable = false;
			
			Gtk.TreeIter iter;
			if (treeEntries.Selection.GetSelected (out iter)) {
				string oldmt = (string) storeMimeTypes.GetValue (iter, 0);
				if (string.IsNullOrEmpty (oldmt))
					storeEntries.Remove (ref iter);
			}
		}

		protected virtual void OnButtonRemoveMimeTypeClicked(object sender, System.EventArgs e)
		{
			TreeIter it;
			if (treeMimeTypes.Selection.GetSelected (out it)) {
				string mt = (string) storeMimeTypes.GetValue (it, 0);
				entry.MimeTypes.Remove (mt);
				storeMimeTypes.Remove (ref it);
				if (!it.Equals (TreeIter.Zero))
					treeMimeTypes.Selection.SelectIter (it);
				NotifyChanged ();
			}
		}

		protected virtual void OnComboLocalesChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			if (comboLocales.Active == 0)
				entry.CurrentLocale = null;
			else
				entry.CurrentLocale = comboLocales.ActiveText;
			
			FillNames ();
		}

		protected virtual void OnComboTypeChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.Type = (DesktopEntryType) comboType.Active;
			UpdateType ();
		}

		protected virtual void OnCheckShowInMenuClicked(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			UpdateShowInMenu ();
		}

		protected virtual void OnEntryNameChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.Name = entryName.Text;
		}

		protected virtual void OnEntryUrlChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.Url = entryUrl.Text;
		}

		protected virtual void OnEntryExecChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.Exec = entryExec.Text;
		}

		protected virtual void OnEntryTryExecChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.TryExec = entryTryExec.Text;
		}

		protected virtual void OnEntryPathChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.Path = entryPath.Text;
		}

		protected virtual void OnEntryCommentChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.Comment = entryComment.Text;
		}

		protected virtual void OnEntryGenericNameChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.GenericName = entryGenericName.Text;
		}

		protected virtual void OnEntryIconChanged(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			NotifyChanged ();
			entry.Icon = entryIcon.Text;
		}

		protected virtual void OnCheckTerminalClicked(object sender, System.EventArgs e)
		{
			if (loading > 0) return;
			entry.Terminal = checkTerminal.Active;
		}

		protected virtual void OnButtonAddEntryClicked(object sender, System.EventArgs e)
		{
			TreeIter it = storeEntries.AppendValues ("", "", "");
			treeEntries.Selection.SelectIter (it);
			entryKeyCell.Editable = true;
			treeEntries.SetCursor (storeEntries.GetPath (it), treeEntries.Columns [0], true);
		}
		
		void HandleOnEditValue (object o, Gtk.EditedArgs e)
		{
			Gtk.TreeIter iter;
			if (storeEntries.GetIterFromString (out iter, e.Path)) {
				string key = (string) storeEntries.GetValue (iter, 1);
				entry.SetEntry (key, e.NewText);
				storeEntries.SetValue (iter, 2, e.NewText);
				NotifyChanged ();
			}
		}
		
		void HandleOnEditKey (object o, Gtk.EditedArgs e)
		{
			entryKeyCell.Editable = false;
			
			Gtk.TreeIter iter;
			if (storeEntries.GetIterFromString (out iter, e.Path)) {
				string key = e.NewText;
				if (key.Length > 0) {
					entry.SetEntry (key, "");
					storeEntries.SetValue (iter, 0, "<b>" + key + "</b>");
					storeEntries.SetValue (iter, 1, key);
					treeEntries.SetCursor (storeEntries.GetPath (iter), treeEntries.Columns [1], true);
					NotifyChanged ();
				}
				else {
					storeEntries.Remove (ref iter);
				}
			}
		}
		
		void HandleOnEditKeyCancelled (object s, EventArgs args)
		{
			entryKeyCell.Editable = false;
			
			Gtk.TreeIter iter;
			if (treeEntries.Selection.GetSelected (out iter)) {
				storeEntries.Remove (ref iter);
			}
		}

		protected virtual void OnButtonRemoveEntryClicked(object sender, System.EventArgs e)
		{
			Gtk.TreeIter iter;
			if (treeEntries.Selection.GetSelected (out iter)) {
				string key = (string) storeEntries.GetValue (iter, 1);
				entry.RemoveEntry (key);
				storeEntries.Remove (ref iter);
				if (!iter.Equals (TreeIter.Zero))
					treeEntries.Selection.SelectIter (iter);
			}
		}
	}
}
