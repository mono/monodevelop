#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	internal partial class RenameConfigDialog
	{
		private global::Gtk.VBox vbox79;

		private global::Gtk.HBox hbox63;

		private global::Gtk.Label label106;

		private global::Gtk.Entry nameEntry;

		private global::Gtk.CheckButton renameChildrenCheck;

		private global::Gtk.Button button9;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.RenameConfigDialog
			this.Name = "MonoDevelop.Ide.Projects.RenameConfigDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Rename Configuration");
			this.TypeHint = ((global::Gdk.WindowTypeHint)(1));
			// Internal child MonoDevelop.Ide.Projects.RenameConfigDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "vbox78";
			w1.BorderWidth = ((uint)(2));
			// Container child vbox78.Gtk.Box+BoxChild
			this.vbox79 = new global::Gtk.VBox();
			this.vbox79.Name = "vbox79";
			this.vbox79.Spacing = 6;
			this.vbox79.BorderWidth = ((uint)(7));
			// Container child vbox79.Gtk.Box+BoxChild
			this.hbox63 = new global::Gtk.HBox();
			this.hbox63.Name = "hbox63";
			this.hbox63.Spacing = 6;
			// Container child hbox63.Gtk.Box+BoxChild
			this.label106 = new global::Gtk.Label();
			this.label106.Name = "label106";
			this.label106.LabelProp = global::Mono.Unix.Catalog.GetString("New name:");
			this.hbox63.Add(this.label106);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox63[this.label106]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox63.Gtk.Box+BoxChild
			this.nameEntry = new global::Gtk.Entry();
			this.nameEntry.Name = "nameEntry";
			this.nameEntry.IsEditable = true;
			this.nameEntry.InvisibleChar = '●';
			this.hbox63.Add(this.nameEntry);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox63[this.nameEntry]));
			w3.Position = 1;
			this.vbox79.Add(this.hbox63);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox79[this.hbox63]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox79.Gtk.Box+BoxChild
			this.renameChildrenCheck = new global::Gtk.CheckButton();
			this.renameChildrenCheck.Name = "renameChildrenCheck";
			this.renameChildrenCheck.Label = global::Mono.Unix.Catalog.GetString("Rename configurations in all solution items");
			this.renameChildrenCheck.Active = true;
			this.renameChildrenCheck.DrawIndicator = true;
			this.renameChildrenCheck.UseUnderline = true;
			this.vbox79.Add(this.renameChildrenCheck);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox79[this.renameChildrenCheck]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			w1.Add(this.vbox79);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(w1[this.vbox79]));
			w6.Position = 0;
			// Internal child MonoDevelop.Ide.Projects.RenameConfigDialog.ActionArea
			global::Gtk.HButtonBox w7 = this.ActionArea;
			w7.Name = "hbuttonbox2";
			w7.Spacing = 6;
			w7.BorderWidth = ((uint)(5));
			w7.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child hbuttonbox2.Gtk.ButtonBox+ButtonBoxChild
			this.button9 = new global::Gtk.Button();
			this.button9.Name = "button9";
			this.button9.UseStock = true;
			this.button9.UseUnderline = true;
			this.button9.Label = "gtk-cancel";
			this.AddActionWidget(this.button9, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w8 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w7[this.button9]));
			w8.Expand = false;
			w8.Fill = false;
			// Container child hbuttonbox2.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			w7.Add(this.buttonOk);
			global::Gtk.ButtonBox.ButtonBoxChild w9 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w7[this.buttonOk]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 371;
			this.DefaultHeight = 149;
			this.Hide();
			this.buttonOk.Clicked += new global::System.EventHandler(this.OnButtonOkClicked);
		}
	}
}
#pragma warning restore 436
