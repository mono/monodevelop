#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	internal partial class SelectReferenceDialog
	{
		private global::Gtk.VBox vbox5;

		private global::Gtk.HPaned hpaned1;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.Alignment alignment2;

		private global::Gtk.VBox boxRefs;

		private global::Gtk.Alignment selectedHeader;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Label label114;

		private global::Gtk.Button RemoveReferenceButton;

		private global::MonoDevelop.Components.ImageView imageAdd;

		private global::Gtk.HBox hbox4;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		private global::Gtk.TreeView ReferencesTreeView;

		private global::Gtk.Button cancelbutton;

		private global::Gtk.Button okbutton;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.SelectReferenceDialog
			this.WidthRequest = 640;
			this.HeightRequest = 520;
			this.Name = "MonoDevelop.Ide.Projects.SelectReferenceDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Edit References");
			this.TypeHint = ((global::Gdk.WindowTypeHint)(1));
			this.BorderWidth = ((uint)(6));
			this.DestroyWithParent = true;
			// Internal child MonoDevelop.Ide.Projects.SelectReferenceDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog-vbox2";
			w1.Spacing = 6;
			// Container child dialog-vbox2.Gtk.Box+BoxChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 12;
			this.vbox5.BorderWidth = ((uint)(6));
			// Container child vbox5.Gtk.Box+BoxChild
			this.hpaned1 = new global::Gtk.HPaned();
			this.hpaned1.CanFocus = true;
			this.hpaned1.Name = "hpaned1";
			this.hpaned1.Position = 590;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.alignment1 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			this.alignment1.RightPadding = ((uint)(3));
			this.hpaned1.Add(this.alignment1);
			global::Gtk.Paned.PanedChild w2 = ((global::Gtk.Paned.PanedChild)(this.hpaned1[this.alignment1]));
			w2.Resize = false;
			w2.Shrink = false;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.alignment2 = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.alignment2.Name = "alignment2";
			this.alignment2.LeftPadding = ((uint)(3));
			// Container child alignment2.Gtk.Container+ContainerChild
			this.boxRefs = new global::Gtk.VBox();
			this.boxRefs.Name = "boxRefs";
			// Container child boxRefs.Gtk.Box+BoxChild
			this.selectedHeader = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.selectedHeader.Name = "selectedHeader";
			// Container child selectedHeader.Gtk.Container+ContainerChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.label114 = new global::Gtk.Label();
			this.label114.Name = "label114";
			this.label114.Xalign = 0F;
			this.label114.LabelProp = global::Mono.Unix.Catalog.GetString("Selected references:");
			this.hbox2.Add(this.label114);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.label114]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.RemoveReferenceButton = new global::Gtk.Button();
			this.RemoveReferenceButton.TooltipMarkup = "Remove";
			this.RemoveReferenceButton.Name = "RemoveReferenceButton";
			this.RemoveReferenceButton.FocusOnClick = false;
			this.RemoveReferenceButton.Relief = ((global::Gtk.ReliefStyle)(2));
			// Container child RemoveReferenceButton.Gtk.Container+ContainerChild
			this.imageAdd = new global::MonoDevelop.Components.ImageView();
			this.imageAdd.Name = "imageAdd";
			this.imageAdd.IconId = "gtk-delete";
			this.imageAdd.IconSize = ((global::Gtk.IconSize)(1));
			this.RemoveReferenceButton.Add(this.imageAdd);
			this.hbox2.Add(this.RemoveReferenceButton);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.RemoveReferenceButton]));
			w5.PackType = ((global::Gtk.PackType)(1));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.selectedHeader.Add(this.hbox2);
			this.boxRefs.Add(this.selectedHeader);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.boxRefs[this.selectedHeader]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child boxRefs.Gtk.Box+BoxChild
			this.hbox4 = new global::Gtk.HBox();
			this.hbox4.HeightRequest = 150;
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 12;
			// Container child hbox4.Gtk.Box+BoxChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow2.Gtk.Container+ContainerChild
			this.ReferencesTreeView = new global::Gtk.TreeView();
			this.ReferencesTreeView.Name = "ReferencesTreeView";
			this.ReferencesTreeView.HeadersVisible = false;
			this.scrolledwindow2.Add(this.ReferencesTreeView);
			this.hbox4.Add(this.scrolledwindow2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox4[this.scrolledwindow2]));
			w9.Position = 0;
			this.boxRefs.Add(this.hbox4);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.boxRefs[this.hbox4]));
			w10.Position = 1;
			this.alignment2.Add(this.boxRefs);
			this.hpaned1.Add(this.alignment2);
			global::Gtk.Paned.PanedChild w12 = ((global::Gtk.Paned.PanedChild)(this.hpaned1[this.alignment2]));
			w12.Shrink = false;
			this.vbox5.Add(this.hpaned1);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hpaned1]));
			w13.Position = 0;
			w1.Add(this.vbox5);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(w1[this.vbox5]));
			w14.Position = 0;
			// Internal child MonoDevelop.Ide.Projects.SelectReferenceDialog.ActionArea
			global::Gtk.HButtonBox w15 = this.ActionArea;
			w15.Name = "dialog-action_area2";
			w15.Spacing = 10;
			w15.BorderWidth = ((uint)(5));
			w15.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog-action_area2.Gtk.ButtonBox+ButtonBoxChild
			this.cancelbutton = new global::Gtk.Button();
			this.cancelbutton.Name = "cancelbutton";
			this.cancelbutton.UseStock = true;
			this.cancelbutton.UseUnderline = true;
			this.cancelbutton.Label = "gtk-cancel";
			this.AddActionWidget(this.cancelbutton, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w16 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w15[this.cancelbutton]));
			w16.Expand = false;
			w16.Fill = false;
			// Container child dialog-action_area2.Gtk.ButtonBox+ButtonBoxChild
			this.okbutton = new global::Gtk.Button();
			this.okbutton.Name = "okbutton";
			this.okbutton.UseStock = true;
			this.okbutton.UseUnderline = true;
			this.okbutton.Label = "gtk-ok";
			this.AddActionWidget(this.okbutton, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w17 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w15[this.okbutton]));
			w17.Position = 1;
			w17.Expand = false;
			w17.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 889;
			this.DefaultHeight = 551;
			this.Hide();
			this.RemoveReferenceButton.Clicked += new global::System.EventHandler(this.RemoveReference);
			this.ReferencesTreeView.KeyReleaseEvent += new global::Gtk.KeyReleaseEventHandler(this.OnReferencesTreeViewKeyReleaseEvent);
			this.ReferencesTreeView.RowActivated += new global::Gtk.RowActivatedHandler(this.OnReferencesTreeViewRowActivated);
		}
	}
}
#pragma warning restore 436
