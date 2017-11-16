#pragma warning disable 436

namespace MonoDevelop.CSharp.ClassOutline
{
	internal partial class OutlineSortingPreferencesDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label label;

		private global::MonoDevelop.Ide.Gui.Components.PriorityList priorityList;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.CSharp.ClassOutline.OutlineSortingPreferencesDialog
			this.Name = "MonoDevelop.CSharp.ClassOutline.OutlineSortingPreferencesDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Document Outline Preferences");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			this.DestroyWithParent = true;
			// Internal child MonoDevelop.CSharp.ClassOutline.OutlineSortingPreferencesDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.label = new global::Gtk.Label();
			this.label.WidthRequest = 400;
			this.label.Name = "label";
			this.label.LabelProp = global::Mono.Unix.Catalog.GetString("Group sorting order when grouping is enabled:");
			this.label.Wrap = true;
			this.vbox2.Add(this.label);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.priorityList = new global::MonoDevelop.Ide.Gui.Components.PriorityList();
			this.priorityList.Name = "priorityList";
			this.vbox2.Add(this.priorityList);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.priorityList]));
			w3.Position = 1;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w4.Position = 0;
			// Internal child MonoDevelop.CSharp.ClassOutline.OutlineSortingPreferencesDialog.ActionArea
			global::Gtk.HButtonBox w5 = this.ActionArea;
			w5.Name = "dialog1_ActionArea";
			w5.Spacing = 10;
			w5.BorderWidth = ((uint)(5));
			w5.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w6 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5[this.buttonCancel]));
			w6.Expand = false;
			w6.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w7 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5[this.buttonOk]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 424;
			this.DefaultHeight = 367;
			this.Hide();
		}
	}
}
#pragma warning restore 436
