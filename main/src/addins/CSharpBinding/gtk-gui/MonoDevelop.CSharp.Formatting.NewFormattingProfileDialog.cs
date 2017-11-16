#pragma warning disable 436

namespace MonoDevelop.CSharp.Formatting
{
	public partial class NewFormattingProfileDialog
	{
		private global::Gtk.VBox vbox4;

		private global::Gtk.Label label3;

		private global::Gtk.Entry entryProfileName;

		private global::Gtk.Label label4;

		private global::Gtk.ComboBox comboboxInitFrom;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.CSharp.Formatting.NewFormattingProfileDialog
			this.Name = "MonoDevelop.CSharp.Formatting.NewFormattingProfileDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("New Profile");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.CSharp.Formatting.NewFormattingProfileDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.Spacing = 6;
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("_Profile name:");
			this.label3.UseUnderline = true;
			this.vbox4.Add(this.label3);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.label3]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.entryProfileName = new global::Gtk.Entry();
			this.entryProfileName.CanFocus = true;
			this.entryProfileName.Name = "entryProfileName";
			this.entryProfileName.IsEditable = true;
			this.entryProfileName.InvisibleChar = '●';
			this.vbox4.Add(this.entryProfileName);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.entryProfileName]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("_Initialize from profile:");
			this.label4.UseUnderline = true;
			this.vbox4.Add(this.label4);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.label4]));
			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.comboboxInitFrom = global::Gtk.ComboBox.NewText();
			this.comboboxInitFrom.Name = "comboboxInitFrom";
			this.vbox4.Add(this.comboboxInitFrom);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.comboboxInitFrom]));
			w5.Position = 3;
			w5.Expand = false;
			w5.Fill = false;
			w1.Add(this.vbox4);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(w1[this.vbox4]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Internal child MonoDevelop.CSharp.Formatting.NewFormattingProfileDialog.ActionArea
			global::Gtk.HButtonBox w7 = this.ActionArea;
			w7.Name = "dialog1_ActionArea";
			w7.Spacing = 10;
			w7.BorderWidth = ((uint)(5));
			w7.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w8 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w7[this.buttonCancel]));
			w8.Expand = false;
			w8.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w9 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w7[this.buttonOk]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 370;
			this.DefaultHeight = 179;
			this.label3.MnemonicWidget = this.entryProfileName;
			this.label4.MnemonicWidget = this.comboboxInitFrom;
			this.Hide();
		}
	}
}
#pragma warning restore 436
