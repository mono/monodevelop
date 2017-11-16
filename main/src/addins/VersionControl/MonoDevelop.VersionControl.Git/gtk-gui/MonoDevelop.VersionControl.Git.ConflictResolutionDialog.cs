#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class ConflictResolutionDialog
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.Label labelTop;

		private global::MonoDevelop.VersionControl.Views.MergeWidget mergeWidget;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		private global::Gtk.Button button24;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.ConflictResolutionDialog
			this.Name = "MonoDevelop.VersionControl.Git.ConflictResolutionDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Merge Conflict Resolution");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.VersionControl.Git.ConflictResolutionDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(9));
			// Container child vbox2.Gtk.Box+BoxChild
			this.labelTop = new global::Gtk.Label();
			this.labelTop.Name = "labelTop";
			this.labelTop.Xalign = 0F;
			this.labelTop.LabelProp = global::Mono.Unix.Catalog.GetString("A merge conflict has been detected in file <b>SomeFile.txt</b>");
			this.labelTop.UseMarkup = true;
			this.vbox2.Add(this.labelTop);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.labelTop]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.mergeWidget = null;
			this.vbox2.Add(this.mergeWidget);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.mergeWidget]));
			w3.Position = 1;
			w1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(w1[this.vbox2]));
			w4.Position = 0;
			// Internal child MonoDevelop.VersionControl.Git.ConflictResolutionDialog.ActionArea
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
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = global::Mono.Unix.Catalog.GetString("Abort Update");
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w6 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5[this.buttonCancel]));
			w6.Expand = false;
			w6.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = global::Mono.Unix.Catalog.GetString("Skip Patch");
			this.AddActionWidget(this.buttonOk, -7);
			global::Gtk.ButtonBox.ButtonBoxChild w7 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5[this.buttonOk]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button24 = new global::Gtk.Button();
			this.button24.CanFocus = true;
			this.button24.Name = "button24";
			this.button24.UseUnderline = true;
			this.button24.Label = global::Mono.Unix.Catalog.GetString("Accept Merge");
			this.AddActionWidget(this.button24, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w8 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5[this.button24]));
			w8.Position = 2;
			w8.Expand = false;
			w8.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 991;
			this.DefaultHeight = 534;
			this.Hide();
		}
	}
}
#pragma warning restore 436
