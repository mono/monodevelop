#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	public partial class ProjectSelectorDialog
	{
		private global::Gtk.VBox vbox4;

		private global::Gtk.Label labelTitle;

		private global::MonoDevelop.Ide.Gui.Components.ProjectSelectorWidget selector;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.ProjectSelectorDialog
			this.Name = "MonoDevelop.Ide.Projects.ProjectSelectorDialog";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.Ide.Projects.ProjectSelectorDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			this.vbox4.BorderWidth = ((uint)(9));
			// Container child vbox4.Gtk.Box+BoxChild
			this.labelTitle = new global::Gtk.Label();
			this.labelTitle.Name = "labelTitle";
			this.labelTitle.Xalign = 0F;
			this.labelTitle.LabelProp = global::Mono.Unix.Catalog.GetString("Select a project or solution:");
			this.vbox4.Add(this.labelTitle);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.labelTitle]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.selector = new global::MonoDevelop.Ide.Gui.Components.ProjectSelectorWidget();
			this.selector.Events = ((global::Gdk.EventMask)(256));
			this.selector.Name = "selector";
			this.selector.ShowCheckboxes = false;
			this.selector.CascadeCheckboxSelection = false;
			this.vbox4.Add(this.selector);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.selector]));
			w3.Position = 1;
			w1.Add(this.vbox4);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(w1[this.vbox4]));
			w4.Position = 0;
			// Internal child MonoDevelop.Ide.Projects.ProjectSelectorDialog.ActionArea
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
			this.DefaultWidth = 492;
			this.DefaultHeight = 466;
			this.Show();
			this.selector.SelectionChanged += new global::System.EventHandler(this.OnSelectorSelectionChanged);
			this.selector.ActiveChanged += new global::System.EventHandler(this.OnSelectorActiveChanged);
		}
	}
}
#pragma warning restore 436
