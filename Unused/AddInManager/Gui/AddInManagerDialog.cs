using System;
using Gtk;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Gui;
using MonoDevelop.Services;

namespace AddInManager
{
	public class AddInManagerDialog : Dialog
	{
		ListStore store;
		AddInDetailsFrame addinDetails;

		public AddInManagerDialog ()
		{
			this.BorderWidth = 12;
			this.Title = GettextCatalog.GetString ("AddInManager");
			this.TransientFor = (Window) WorkbenchSingleton.Workbench;
			this.SetDefaultSize (400, 350);

			ScrolledWindow sw = new ScrolledWindow ();
			sw.ShadowType = ShadowType.In;
			TreeView tv = new TreeView ();
			tv.Selection.Changed += new EventHandler (OnSelectionChanged);
			tv.RowActivated += new RowActivatedHandler (OnRowActivated);

			CellRendererToggle toggle = new CellRendererToggle ();
			toggle.Toggled += OnCellToggled;
			tv.AppendColumn (GettextCatalog.GetString ("Enabled"), toggle, "active", 0);
			tv.AppendColumn (GettextCatalog.GetString ("AddIn Name"), new CellRendererText (), "text", 1);
			tv.AppendColumn (GettextCatalog.GetString ("Version"), new CellRendererText (), "text", 2);
			sw.Add (tv);

			this.AddButton (Gtk.Stock.Close, ResponseType.Close);
	
			LoadAddIns ();
			tv.Model = store;
			this.VBox.Add (sw);

			addinDetails = new AddInDetailsFrame ();
			this.VBox.Add (addinDetails);
			this.ShowAll ();
		}

		void LoadAddIns ()
		{
			store = new ListStore (typeof (bool), typeof (string), typeof (string), typeof (AddIn));
			AddInCollection addins = AddInTreeSingleton.AddInTree.AddIns;

			foreach (AddIn a in addins)
				store.AppendValues (true, a.Name, a.Version, a);
		}

		void OnCellToggled (object sender, ToggledArgs a)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, a.Path))
				Toggle (iter);
		}

		void OnSelectionChanged (object sender, EventArgs a)
		{
			TreeIter iter;
			TreeModel model;

			if (((TreeSelection)sender).GetSelected (out model, out iter))
				addinDetails.SetAddin ((AddIn) model.GetValue (iter, 3));
			else
				addinDetails.Clear ();
		}

		void OnRowActivated (object sender, RowActivatedArgs a)
		{
			TreeIter iter;
			if (store.GetIter (out iter, a.Path))
				Toggle (iter);
		}

		void Toggle (TreeIter iter)
		{
			bool val = (bool) store.GetValue (iter, 0);
			store.SetValue (iter, 0, !val);
		}
	}
}

