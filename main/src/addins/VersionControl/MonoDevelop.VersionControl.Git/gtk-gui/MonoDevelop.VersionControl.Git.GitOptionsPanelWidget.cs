#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class GitOptionsPanelWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.CheckButton checkStashBranch;

		private global::Gtk.Label label1;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.VBox vbox3;

		private global::Gtk.CheckButton checkRebase;

		private global::Gtk.CheckButton checkStashUpdate;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.GitOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.VersionControl.Git.GitOptionsPanelWidget";
			// Container child MonoDevelop.VersionControl.Git.GitOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkStashBranch = new global::Gtk.CheckButton();
			this.checkStashBranch.CanFocus = true;
			this.checkStashBranch.Name = "checkStashBranch";
			this.checkStashBranch.Label = global::Mono.Unix.Catalog.GetString("Automatically stash/unstash changes when switching branches");
			this.checkStashBranch.DrawIndicator = true;
			this.checkStashBranch.UseUnderline = true;
			this.vbox2.Add(this.checkStashBranch);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkStashBranch]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Behavior of the Update command:");
			this.vbox2.Add(this.label1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.label1]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment1 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			this.alignment1.LeftPadding = ((uint)(12));
			// Container child alignment1.Gtk.Container+ContainerChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkRebase = new global::Gtk.CheckButton();
			this.checkRebase.CanFocus = true;
			this.checkRebase.Name = "checkRebase";
			this.checkRebase.Label = global::Mono.Unix.Catalog.GetString("Use the Rebase option for merging");
			this.checkRebase.DrawIndicator = true;
			this.checkRebase.UseUnderline = true;
			this.vbox3.Add(this.checkRebase);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.checkRebase]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkStashUpdate = new global::Gtk.CheckButton();
			this.checkStashUpdate.CanFocus = true;
			this.checkStashUpdate.Name = "checkStashUpdate";
			this.checkStashUpdate.Label = global::Mono.Unix.Catalog.GetString("Automatically stash/unstash local changes");
			this.checkStashUpdate.DrawIndicator = true;
			this.checkStashUpdate.UseUnderline = true;
			this.vbox3.Add(this.checkStashUpdate);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.checkStashUpdate]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			this.alignment1.Add(this.vbox3);
			this.vbox2.Add(this.alignment1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment1]));
			w6.Position = 2;
			w6.Expand = false;
			w6.Fill = false;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
