
using System;

namespace Stetic.Editor
{
	public class FlagsSelectorDialog: IDisposable
	{
		[Glade.Widget] Gtk.TreeView treeView;
		[Glade.Widget ("FlagsSelectorDialog")] Gtk.Dialog dialog;
		Gtk.ListStore store;
		Gtk.Window parent;
		uint flags;
		
		public FlagsSelectorDialog (Gtk.Window parent, EnumDescriptor enumDesc, uint flags, string title)
		{
			this.flags = flags;
			this.parent = parent;

			Glade.XML xml = new Glade.XML (null, "stetic.glade", "FlagsSelectorDialog", null);
			xml.Autoconnect (this);
			
			store = new Gtk.ListStore (typeof(bool), typeof(string), typeof(uint));
			treeView.Model = store;
			
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			
			Gtk.CellRendererToggle tog = new Gtk.CellRendererToggle ();
			tog.Toggled += new Gtk.ToggledHandler (OnToggled);
			col.PackStart (tog, false);
			col.AddAttribute (tog, "active", 0);
			
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", 1);
			
			treeView.AppendColumn (col);
			
			foreach (Enum value in enumDesc.Values) {
				EnumValue eval = enumDesc[value];
				if (eval.Label == "")
					continue;
				uint val = (uint) Convert.ToInt32 (eval.Value);
				store.AppendValues (((flags & val) != 0), eval.Label, val);
			}
		}
		
		public int Run ()
		{
			dialog.ShowAll ();
			dialog.TransientFor = parent;
			return dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
		
		void OnToggled (object s, Gtk.ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;
			
			bool oldValue = (bool) store.GetValue (iter, 0);
			uint flag = (uint) store.GetValue (iter, 2);
			store.SetValue (iter, 0, !oldValue);
			
			if (oldValue)
				flags &= ~flag;
			else
				flags |= flag;
		}

		public uint Value {
			get { return flags; }
		}
	}
}
