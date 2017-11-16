#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	internal partial class AddExternalFileDialog
	{
		private global::Gtk.HBox hbox1;

		private global::Gtk.VBox vbox3;

		private global::MonoDevelop.Components.ImageView iconQuestion;

		private global::Gtk.VBox vbox2;

		private global::Gtk.Label labelTitle;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.VBox vbox4;

		private global::Gtk.RadioButton radioKeep;

		private global::Gtk.Label labelKeep;

		private global::Gtk.RadioButton radioCopy;

		private global::Gtk.Label label4;

		private global::Gtk.RadioButton radioMove;

		private global::Gtk.Label label5;

		private global::Gtk.RadioButton radioLink;

		private global::Gtk.Label label6;

		private global::Gtk.CheckButton checkApplyAll;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.AddExternalFileDialog
			this.Name = "MonoDevelop.Ide.Projects.AddExternalFileDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Add File to Folder");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.Ide.Projects.AddExternalFileDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			this.hbox1.BorderWidth = ((uint)(9));
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.iconQuestion = new global::MonoDevelop.Components.ImageView();
			this.iconQuestion.Name = "iconQuestion";
			this.iconQuestion.IconId = "gtk-dialog-question";
			this.iconQuestion.IconSize = ((global::Gtk.IconSize)(6));
			this.vbox3.Add(this.iconQuestion);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.iconQuestion]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			this.hbox1.Add(this.vbox3);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox3]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 18;
			this.vbox2.BorderWidth = ((uint)(3));
			// Container child vbox2.Gtk.Box+BoxChild
			this.labelTitle = new global::Gtk.Label();
			this.labelTitle.WidthRequest = 450;
			this.labelTitle.Name = "labelTitle";
			this.labelTitle.Xalign = 0F;
			this.labelTitle.LabelProp = global::Mono.Unix.Catalog.GetString("The file {0} is outside the target directory. What would you like to do?");
			this.labelTitle.UseMarkup = true;
			this.labelTitle.Wrap = true;
			this.vbox2.Add(this.labelTitle);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.labelTitle]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment1 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			this.alignment1.LeftPadding = ((uint)(20));
			// Container child alignment1.Gtk.Container+ContainerChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.radioKeep = new global::Gtk.RadioButton("");
			this.radioKeep.CanFocus = true;
			this.radioKeep.Name = "radioKeep";
			this.radioKeep.DrawIndicator = true;
			this.radioKeep.UseUnderline = true;
			this.radioKeep.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.radioKeep.Remove(this.radioKeep.Child);
			// Container child radioKeep.Gtk.Container+ContainerChild
			this.labelKeep = new global::Gtk.Label();
			this.labelKeep.WidthRequest = 376;
			this.labelKeep.Name = "labelKeep";
			this.labelKeep.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Keep</b> the file in its the current subdirectory ({0})");
			this.labelKeep.UseMarkup = true;
			this.labelKeep.Wrap = true;
			this.radioKeep.Add(this.labelKeep);
			this.vbox4.Add(this.radioKeep);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.radioKeep]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.radioCopy = new global::Gtk.RadioButton("");
			this.radioCopy.CanFocus = true;
			this.radioCopy.Name = "radioCopy";
			this.radioCopy.DrawIndicator = true;
			this.radioCopy.UseUnderline = true;
			this.radioCopy.Group = this.radioKeep.Group;
			this.radioCopy.Remove(this.radioCopy.Child);
			// Container child radioCopy.Gtk.Container+ContainerChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Copy</b> the file to the directory");
			this.label4.UseMarkup = true;
			this.radioCopy.Add(this.label4);
			this.vbox4.Add(this.radioCopy);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.radioCopy]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.radioMove = new global::Gtk.RadioButton("");
			this.radioMove.CanFocus = true;
			this.radioMove.Name = "radioMove";
			this.radioMove.DrawIndicator = true;
			this.radioMove.UseUnderline = true;
			this.radioMove.Group = this.radioKeep.Group;
			this.radioMove.Remove(this.radioMove.Child);
			// Container child radioMove.Gtk.Container+ContainerChild
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Move</b> the file to the directory");
			this.label5.UseMarkup = true;
			this.radioMove.Add(this.label5);
			this.vbox4.Add(this.radioMove);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.radioMove]));
			w10.Position = 2;
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.radioLink = new global::Gtk.RadioButton("");
			this.radioLink.CanFocus = true;
			this.radioLink.Name = "radioLink";
			this.radioLink.DrawIndicator = true;
			this.radioLink.UseUnderline = true;
			this.radioLink.Group = this.radioKeep.Group;
			this.radioLink.Remove(this.radioLink.Child);
			// Container child radioLink.Gtk.Container+ContainerChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Add a link</b> to the file");
			this.label6.UseMarkup = true;
			this.radioLink.Add(this.label6);
			this.vbox4.Add(this.radioLink);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.radioLink]));
			w12.Position = 3;
			w12.Expand = false;
			w12.Fill = false;
			this.alignment1.Add(this.vbox4);
			this.vbox2.Add(this.alignment1);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment1]));
			w14.Position = 1;
			w14.Expand = false;
			w14.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkApplyAll = new global::Gtk.CheckButton();
			this.checkApplyAll.CanFocus = true;
			this.checkApplyAll.Name = "checkApplyAll";
			this.checkApplyAll.Label = global::Mono.Unix.Catalog.GetString("Use the same action for all selected files");
			this.checkApplyAll.DrawIndicator = true;
			this.checkApplyAll.UseUnderline = true;
			this.vbox2.Add(this.checkApplyAll);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkApplyAll]));
			w15.Position = 2;
			w15.Expand = false;
			w15.Fill = false;
			this.hbox1.Add(this.vbox2);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox2]));
			w16.Position = 1;
			w16.Expand = false;
			w16.Fill = false;
			w1.Add(this.hbox1);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(w1[this.hbox1]));
			w17.Position = 0;
			// Internal child MonoDevelop.Ide.Projects.AddExternalFileDialog.ActionArea
			global::Gtk.HButtonBox w18 = this.ActionArea;
			w18.Name = "dialog1_ActionArea";
			w18.Spacing = 10;
			w18.BorderWidth = ((uint)(11));
			w18.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w19 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w18[this.buttonCancel]));
			w19.Expand = false;
			w19.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w20 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w18[this.buttonOk]));
			w20.Position = 1;
			w20.Expand = false;
			w20.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 536;
			this.DefaultHeight = 286;
			this.radioKeep.Hide();
			this.checkApplyAll.Hide();
			this.Show();
		}
	}
}
#pragma warning restore 436
