#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	internal partial class ApplyPolicyDialog
	{
		private global::Gtk.VBox vbox3;

		private global::Gtk.VBox vbox2;

		private global::Gtk.RadioButton radioCustom;

		private global::Gtk.Alignment boxCustom;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label2;

		private global::Gtk.ComboBox combPolicies;

		private global::Gtk.RadioButton radioFile;

		private global::Gtk.Alignment boxFile;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label label3;

		private global::MonoDevelop.Components.FileEntry fileEntry;

		private global::Gtk.VBox vbox4;

		private global::Gtk.Label labelChangesTitle;

		private global::Gtk.ScrolledWindow policiesScroll;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.ApplyPolicyDialog
			this.Name = "MonoDevelop.Ide.Projects.ApplyPolicyDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Apply Policies");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.Ide.Projects.ApplyPolicyDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 16;
			this.vbox3.BorderWidth = ((uint)(12));
			// Container child vbox3.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.radioCustom = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Apply stock or custom policy set"));
			this.radioCustom.CanFocus = true;
			this.radioCustom.Name = "radioCustom";
			this.radioCustom.DrawIndicator = true;
			this.radioCustom.UseUnderline = true;
			this.radioCustom.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.vbox2.Add(this.radioCustom);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.radioCustom]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.boxCustom = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.boxCustom.Name = "boxCustom";
			this.boxCustom.LeftPadding = ((uint)(42));
			// Container child boxCustom.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Policy:");
			this.hbox1.Add(this.label2);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label2]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.combPolicies = global::Gtk.ComboBox.NewText();
			this.combPolicies.Name = "combPolicies";
			this.hbox1.Add(this.combPolicies);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.combPolicies]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			this.boxCustom.Add(this.hbox1);
			this.vbox2.Add(this.boxCustom);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.boxCustom]));
			w6.Position = 1;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.radioFile = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Apply policies from file"));
			this.radioFile.CanFocus = true;
			this.radioFile.Name = "radioFile";
			this.radioFile.DrawIndicator = true;
			this.radioFile.UseUnderline = true;
			this.radioFile.Group = this.radioCustom.Group;
			this.vbox2.Add(this.radioFile);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.radioFile]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.boxFile = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.boxFile.Name = "boxFile";
			this.boxFile.LeftPadding = ((uint)(42));
			// Container child boxFile.Gtk.Container+ContainerChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("File:");
			this.hbox2.Add(this.label3);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.label3]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.fileEntry = new global::MonoDevelop.Components.FileEntry();
			this.fileEntry.Name = "fileEntry";
			this.hbox2.Add(this.fileEntry);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.fileEntry]));
			w9.Position = 1;
			this.boxFile.Add(this.hbox2);
			this.vbox2.Add(this.boxFile);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.boxFile]));
			w11.Position = 3;
			w11.Expand = false;
			w11.Fill = false;
			this.vbox3.Add(this.vbox2);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.vbox2]));
			w12.Position = 0;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.labelChangesTitle = new global::Gtk.Label();
			this.labelChangesTitle.Name = "labelChangesTitle";
			this.labelChangesTitle.Xalign = 0F;
			this.labelChangesTitle.LabelProp = global::Mono.Unix.Catalog.GetString("Policies to set or replace:");
			this.vbox4.Add(this.labelChangesTitle);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.labelChangesTitle]));
			w13.Position = 0;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.policiesScroll = new global::Gtk.ScrolledWindow();
			this.policiesScroll.CanFocus = true;
			this.policiesScroll.Name = "policiesScroll";
			this.policiesScroll.ShadowType = ((global::Gtk.ShadowType)(1));
			this.vbox4.Add(this.policiesScroll);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.policiesScroll]));
			w14.Position = 1;
			this.vbox3.Add(this.vbox4);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.vbox4]));
			w15.Position = 1;
			w1.Add(this.vbox3);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(w1[this.vbox3]));
			w16.Position = 0;
			// Internal child MonoDevelop.Ide.Projects.ApplyPolicyDialog.ActionArea
			global::Gtk.HButtonBox w17 = this.ActionArea;
			w17.Name = "dialog1_ActionArea";
			w17.Spacing = 10;
			w17.BorderWidth = ((uint)(5));
			w17.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w18 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w17[this.buttonCancel]));
			w18.Expand = false;
			w18.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = global::Mono.Unix.Catalog.GetString("_Apply policies");
			w17.Add(this.buttonOk);
			global::Gtk.ButtonBox.ButtonBoxChild w19 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w17[this.buttonOk]));
			w19.Position = 1;
			w19.Expand = false;
			w19.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 475;
			this.DefaultHeight = 325;
			this.Hide();
			this.radioCustom.Toggled += new global::System.EventHandler(this.OnRadioCustomToggled);
			this.combPolicies.Changed += new global::System.EventHandler(this.OnCombPoliciesChanged);
			this.buttonOk.Clicked += new global::System.EventHandler(this.OnButtonOkClicked);
		}
	}
}
#pragma warning restore 436
