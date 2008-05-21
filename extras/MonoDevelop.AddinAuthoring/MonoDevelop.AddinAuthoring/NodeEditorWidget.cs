
using System;
using System.Collections;
using Gtk;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.AddinAuthoring
{
	public class NodeEditorWidget: VBox
	{
		ExtensionNodeDescription node;
		
		ComboBox insBeforeCombo;
		ComboBox insAfterCombo;
		Gtk.Tooltips tips;
		DotNetProject project;
		
		Hashtable atts = new Hashtable ();
		
		public NodeEditorWidget (DotNetProject project, AddinRegistry reg, ExtensionNodeType ntype, AddinDescription parentAddinDescription, string parentPath, ExtensionNodeDescription node)
		{
			this.node = node;
			this.project = project;
			Spacing = 6;
			tips = new Tooltips ();
			
			// Header
			
			Label label = new Label ();
			label.Wrap = true;
			label.WidthRequest = 480;
			string txt = "<b>" + node.NodeName + "</b>";
			if (ntype.Description.Length > 0)
				txt += "\n" + GLib.Markup.EscapeText (ntype.Description);
			label.Markup = txt;
			label.Xalign = 0f;
			PackStart (label, false, false, 0);
			PackStart (new HSeparator (), false, false, 0);
			
			// Attributes
			
			Table table = new Table ((uint) ntype.Attributes.Count + 1, 2, false);
			table.RowSpacing = 6;
			table.ColumnSpacing = 6;
			
			uint r = 1;
			AddAttribute (table, "id", "System.String", AddinManager.CurrentLocalizer.GetString ("Identifier of the extension node"), false, 0);
			Console.WriteLine ("pp: " + ntype.TypeName + " " + ntype.Attributes.Count);
			foreach (NodeTypeAttribute att in ntype.Attributes) {
				AddAttribute (table, att.Name, att.Type, att.Description, att.Required, r);
				r++;
			}
			PackStart (table, false, false, 0);
			PackStart (new HSeparator (), false, false, 0);
			
			// Insert before/after combos
			
			table = new Table (2, 2, false);
			table.RowSpacing = 6;
			table.ColumnSpacing = 6;
			
			label = new Label (AddinManager.CurrentLocalizer.GetString ("Insert before:"));
			label.Xalign = 0f;
			table.Attach (label, 0, 1, 0, 1, AttachOptions.Shrink|AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
			label = new Label (AddinManager.CurrentLocalizer.GetString ("Insert after:"));
			label.Xalign = 0f;
			table.Attach (label, 0, 1, 1, 2, AttachOptions.Shrink|AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
			
			insBeforeCombo = ComboBox.NewText ();
			insBeforeCombo.AppendText (AddinManager.CurrentLocalizer.GetString ("(None)"));
			insBeforeCombo.Active = 0;
			table.Attach (insBeforeCombo, 1, 2, 0, 1);
			
			insAfterCombo = ComboBox.NewText ();
			insAfterCombo.AppendText (AddinManager.CurrentLocalizer.GetString ("(None)"));
			insAfterCombo.Active = 0;
			table.Attach (insAfterCombo, 1, 2, 1, 2);
			
			int n = 1;
			foreach (ExtensionNodeDescription en in AddinData.GetExtensionNodes (reg, parentAddinDescription, parentPath)) {
				if (en.Id.Length > 0) {
					insBeforeCombo.AppendText (en.Id);
					insAfterCombo.AppendText (en.Id);
					if (en.Id == node.InsertBefore)
						insBeforeCombo.Active = n;
					if (en.Id == node.InsertAfter)
						insAfterCombo.Active = n;
				}
				n++;
			}
			
			PackStart (table, false, false, 0);
			
			ShowAll ();
		}
		
		void AddAttribute (Table table, string name, string type, string desc, bool required, uint r)
		{
			Label label = new Label ();
			if (required)
				label.Markup = "<b>" + name + ":" + "</b>";
			else
				label.Text = name + ":";
			
			label.Xalign = 0f;
			EventBox box = new EventBox ();
			box.Add (label);
			table.Attach (box, 0, 1, r, r + 1, AttachOptions.Shrink|AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
			
			Widget w = CreateWidget (type, node.GetAttribute (name));
			table.Attach (w, 1, 2, r, r + 1);
			atts [w] = name;
			
			if (desc.Length > 0) {
				tips.SetTip (box, desc, desc);
				tips.SetTip (w, desc, desc);
			}
		}
		
		Gtk.Widget CreateWidget (string type, string value)
		{
			switch (type)
			{
			case "System.Boolean":
				CheckButton bt = new CheckButton ();
				bt.Active = value.ToLower () == "true";
				return bt;
			case "System.Type":
				return new TypeSelector (project, value);
			}
			return new Entry (value);
		}
		
		string GetValue (Gtk.Widget w)
		{
			if (w is CheckButton)
				return ((CheckButton)w).Active.ToString ();
			if (w is TypeSelector)
				return ((TypeSelector)w).TypeName;
			return ((Entry)w).Text;
		}
		
		public void Save ()
		{
			foreach (DictionaryEntry e in atts)
				node.SetAttribute ((string) e.Value, GetValue ((Widget)e.Key));
			
			if (insBeforeCombo.Active != 0)
				node.InsertBefore = insBeforeCombo.ActiveText;
			else
				node.InsertBefore = string.Empty;
			
			if (insAfterCombo.Active != 0)
				node.InsertAfter = insAfterCombo.ActiveText;
			else
				node.InsertAfter = string.Empty;
		}
	}
}
