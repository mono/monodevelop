#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	internal partial class DeleteConfigDialog
	{
		private global::Gtk.HBox hbox64;

		private global::MonoDevelop.Components.ImageView imageQuestion;

		private global::Gtk.VBox vbox80;

		private global::Gtk.Label label107;

		private global::Gtk.CheckButton deleteChildrenCheck;

		private global::Gtk.Button button11;

		private global::Gtk.Button button12;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.DeleteConfigDialog
			this.Name = "MonoDevelop.Ide.Projects.DeleteConfigDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Delete Configuration");
			this.TypeHint = ((global::Gdk.WindowTypeHint)(1));
			// Internal child MonoDevelop.Ide.Projects.DeleteConfigDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog-vbox7";
			// Container child dialog-vbox7.Gtk.Box+BoxChild
			this.hbox64 = new global::Gtk.HBox();
			this.hbox64.Name = "hbox64";
			this.hbox64.Spacing = 12;
			this.hbox64.BorderWidth = ((uint)(12));
			// Container child hbox64.Gtk.Box+BoxChild
			this.imageQuestion = new global::MonoDevelop.Components.ImageView();
			this.imageQuestion.Name = "imageQuestion";
			this.imageQuestion.Yalign = 0F;
			this.imageQuestion.IconId = "gtk-dialog-question";
			this.imageQuestion.IconSize = ((global::Gtk.IconSize)(6));
			this.hbox64.Add(this.imageQuestion);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox64[this.imageQuestion]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox64.Gtk.Box+BoxChild
			this.vbox80 = new global::Gtk.VBox();
			this.vbox80.Name = "vbox80";
			this.vbox80.Spacing = 6;
			// Container child vbox80.Gtk.Box+BoxChild
			this.label107 = new global::Gtk.Label();
			this.label107.Name = "label107";
			this.label107.Xalign = 0F;
			this.label107.LabelProp = global::Mono.Unix.Catalog.GetString("Are you sure you want to delete this configuration?");
			this.vbox80.Add(this.label107);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox80[this.label107]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox80.Gtk.Box+BoxChild
			this.deleteChildrenCheck = new global::Gtk.CheckButton();
			this.deleteChildrenCheck.Name = "deleteChildrenCheck";
			this.deleteChildrenCheck.Label = global::Mono.Unix.Catalog.GetString("Delete also configurations in solution items");
			this.deleteChildrenCheck.Active = true;
			this.deleteChildrenCheck.DrawIndicator = true;
			this.deleteChildrenCheck.UseUnderline = true;
			this.vbox80.Add(this.deleteChildrenCheck);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox80[this.deleteChildrenCheck]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			this.hbox64.Add(this.vbox80);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox64[this.vbox80]));
			w5.Position = 1;
			w1.Add(this.hbox64);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(w1[this.hbox64]));
			w6.Position = 2;
			// Internal child MonoDevelop.Ide.Projects.DeleteConfigDialog.ActionArea
			global::Gtk.HButtonBox w7 = this.ActionArea;
			w7.Name = "dialog-action_area7";
			w7.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog-action_area7.Gtk.ButtonBox+ButtonBoxChild
			this.button11 = new global::Gtk.Button();
			this.button11.Name = "button11";
			this.button11.UseStock = true;
			this.button11.UseUnderline = true;
			this.button11.Label = "gtk-no";
			this.AddActionWidget(this.button11, -9);
			global::Gtk.ButtonBox.ButtonBoxChild w8 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w7[this.button11]));
			w8.Expand = false;
			w8.Fill = false;
			// Container child dialog-action_area7.Gtk.ButtonBox+ButtonBoxChild
			this.button12 = new global::Gtk.Button();
			this.button12.CanDefault = true;
			this.button12.Name = "button12";
			this.button12.UseStock = true;
			this.button12.UseUnderline = true;
			this.button12.Label = "gtk-yes";
			this.AddActionWidget(this.button12, -8);
			global::Gtk.ButtonBox.ButtonBoxChild w9 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w7[this.button12]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 381;
			this.DefaultHeight = 128;
			this.Hide();
		}
	}
}
#pragma warning restore 436
