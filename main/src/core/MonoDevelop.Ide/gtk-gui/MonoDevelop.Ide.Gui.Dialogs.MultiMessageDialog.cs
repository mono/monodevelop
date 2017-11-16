#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class MultiMessageDialog
	{
		private global::Gtk.ScrolledWindow scrolled;

		private global::Gtk.VBox msgBox;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.Dialogs.MultiMessageDialog
			this.Name = "MonoDevelop.Ide.Gui.Dialogs.MultiMessageDialog";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.Ide.Gui.Dialogs.MultiMessageDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.scrolled = new global::Gtk.ScrolledWindow();
			this.scrolled.Name = "scrolled";
			this.scrolled.HscrollbarPolicy = ((global::Gtk.PolicyType)(2));
			this.scrolled.BorderWidth = ((uint)(6));
			// Container child scrolled.Gtk.Container+ContainerChild
			global::Gtk.Viewport w2 = new global::Gtk.Viewport();
			w2.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child GtkViewport.Gtk.Container+ContainerChild
			this.msgBox = new global::Gtk.VBox();
			this.msgBox.Name = "msgBox";
			this.msgBox.Spacing = 12;
			w2.Add(this.msgBox);
			this.scrolled.Add(w2);
			w1.Add(this.scrolled);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(w1[this.scrolled]));
			w5.Position = 0;
			// Internal child MonoDevelop.Ide.Gui.Dialogs.MultiMessageDialog.ActionArea
			global::Gtk.HButtonBox w6 = this.ActionArea;
			w6.Name = "dialog1_ActionArea";
			w6.Spacing = 6;
			w6.BorderWidth = ((uint)(5));
			w6.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w7 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w6[this.buttonOk]));
			w7.Expand = false;
			w7.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 563;
			this.DefaultHeight = 346;
			this.Hide();
		}
	}
}
#pragma warning restore 436
