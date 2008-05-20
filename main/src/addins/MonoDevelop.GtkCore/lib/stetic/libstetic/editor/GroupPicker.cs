using Gtk;
using Gdk;
using GLib;
using System;
using System.Collections;
using System.Reflection;
using Mono.Unix;

namespace Stetic.Editor {

	[PropertyEditor ("Group", "Changed")]
	class GroupPicker : Gtk.HBox, IPropertyEditor {

		Gtk.ComboBox combo;
		IRadioGroupManager manager;
		ArrayList values;
		string group;

		const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		public GroupPicker () : base (false, 0)
		{
		}
		
		public void Initialize (PropertyDescriptor prop)
		{
		}
		
		public void AttachObject (object ob)
		{
			ob = ObjectWrapper.Lookup (ob);
			
			IRadioGroupManagerProvider provider = ob as IRadioGroupManagerProvider;
			
			if (provider == null)
				throw new ArgumentException ("The class " + ob.GetType() + " does not implement IRadioGroupManagerProvider");

			manager = provider.GetGroupManager ();
			manager.GroupsChanged += GroupsChanged;
			GroupsChanged ();
		}

		public override void Dispose ()
		{
			manager.GroupsChanged -= GroupsChanged;
			base.Dispose ();
		}

		void GroupsChanged ()
		{
			if (combo != null) {
				combo.Changed -= combo_Changed;
				Remove (combo);
			}

			combo = Gtk.ComboBox.NewText ();
			combo.Changed += combo_Changed;
#if GTK_SHARP_2_6
			combo.RowSeparatorFunc = RowSeparatorFunc;
#endif
			combo.Show ();
			PackStart (combo, true, true, 0);

			values = new ArrayList ();
			int i = 0;
			foreach (string name in manager.GroupNames) {
				values.Add (name);
				combo.AppendText (name);
				if (name == group)
					combo.Active = i;
				i++;
			}

#if GTK_SHARP_2_6
			combo.AppendText ("");
#endif

			combo.AppendText (Catalog.GetString ("Rename Group..."));
			combo.AppendText (Catalog.GetString ("New Group..."));
		}

#if GTK_SHARP_2_6
		bool RowSeparatorFunc (Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			GLib.Value val = new GLib.Value ();
			model.GetValue (iter, 0, ref val);
			bool sep = ((string)val) == "";
			val.Dispose ();
			return sep;
		}
#endif

		public object Value {
			get {
				return group;
			}
			set {
				int index = values.IndexOf ((string) value);
				if (index != -1) {
					combo.Active = index;
					group = values[index] as string;
				}
			}
		}

		public event EventHandler ValueChanged;

		void combo_Changed (object o, EventArgs args)
		{
			if (combo.Active >= values.Count) {
				doDialog ();
				return;
			}

			group = values[combo.Active] as string;
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		void doDialog ()
		{
#if GTK_SHARP_2_6
			bool rename = combo.Active == values.Count + 1;
#else
			bool rename = combo.Active == values.Count;
#endif
			Gtk.Dialog dialog = new Gtk.Dialog (
				rename ? Catalog.GetString ("Rename Group") : Catalog.GetString ("New Group"),
				combo.Toplevel as Gtk.Window,
				Gtk.DialogFlags.Modal | Gtk.DialogFlags.NoSeparator, 
				Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
				Gtk.Stock.Ok, Gtk.ResponseType.Ok);
			dialog.DefaultResponse = Gtk.ResponseType.Ok;
			dialog.HasSeparator = false;
			dialog.BorderWidth = 12;
			dialog.VBox.Spacing = 18;
			dialog.VBox.BorderWidth = 0;

			Gtk.HBox hbox = new Gtk.HBox (false, 12);
			Gtk.Label label = new Gtk.Label (rename ? Catalog.GetString ("_New name:") : Catalog.GetString ("_Name:"));
			Gtk.Entry entry = new Gtk.Entry ();
			label.MnemonicWidget = entry;
			hbox.PackStart (label, false, false, 0);
			entry.ActivatesDefault = true;
			if (rename)
				entry.Text = group;
			hbox.PackStart (entry, true, true, 0);
			dialog.VBox.PackStart (hbox, false, false, 0);

			dialog.ShowAll ();
			// Have to set this *after* ShowAll
			dialog.ActionArea.BorderWidth = 0;
			Gtk.ResponseType response = (Gtk.ResponseType)dialog.Run ();
			if (response == Gtk.ResponseType.Cancel || entry.Text.Length == 0) {
				dialog.Destroy ();
				Value = group; // reset combo.Active
				return;
			}

			string oldname = group;
			group = entry.Text;
			dialog.Destroy ();

			// FIXME: check that the new name doesn't already exist

			// This will trigger a GroupsChanged, which will eventually
			// update combo.Active
			if (rename)
				manager.Rename (oldname, group);
			else
				manager.Add (group);
		}
	}
}
