#pragma warning disable 436

namespace MonoDevelop.VersionControl.Git
{
	internal partial class GitConfigurationDialog
	{
		private global::Gtk.Notebook notebook1;

		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView listBranches;

		private global::Gtk.VBox vbox3;

		private global::Gtk.Button buttonAddBranch;

		private global::Gtk.Button buttonEditBranch;

		private global::Gtk.Button buttonRemoveBranch;

		private global::Gtk.Button buttonSetDefaultBranch;

		private global::Gtk.Label label1;

		private global::Gtk.VBox vbox6;

		private global::Gtk.HBox hbox3;

		private global::Gtk.ScrolledWindow GtkScrolledWindow2;

		private global::Gtk.TreeView listTags;

		private global::Gtk.VBox vbox7;

		private global::Gtk.Button buttonAddTag;

		private global::Gtk.Button buttonPushTag;

		private global::Gtk.Button buttonRemoveTag;

		private global::Gtk.Label label5;

		private global::Gtk.VBox vbox4;

		private global::Gtk.HBox hbox2;

		private global::Gtk.ScrolledWindow GtkScrolledWindow1;

		private global::Gtk.TreeView treeRemotes;

		private global::Gtk.VBox vbox5;

		private global::Gtk.Button buttonAddRemote;

		private global::Gtk.Button buttonEditRemote;

		private global::Gtk.Button buttonRemoveRemote;

		private global::Gtk.HSeparator hseparator2;

		private global::Gtk.Button buttonTrackRemote;

		private global::Gtk.Button buttonFetch;

		private global::Gtk.Label label2;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Git.GitConfigurationDialog
			this.Name = "MonoDevelop.VersionControl.Git.GitConfigurationDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Git Repository Configuration");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.VersionControl.Git.GitConfigurationDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.notebook1 = new global::Gtk.Notebook();
			this.notebook1.CanFocus = true;
			this.notebook1.Name = "notebook1";
			this.notebook1.CurrentPage = 0;
			this.notebook1.BorderWidth = ((uint)(9));
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(9));
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.listBranches = new global::Gtk.TreeView();
			this.listBranches.CanFocus = true;
			this.listBranches.Name = "listBranches";
			this.GtkScrolledWindow.Add(this.listBranches);
			this.hbox1.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.GtkScrolledWindow]));
			w3.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.buttonAddBranch = new global::Gtk.Button();
			this.buttonAddBranch.CanFocus = true;
			this.buttonAddBranch.Name = "buttonAddBranch";
			this.buttonAddBranch.UseStock = true;
			this.buttonAddBranch.UseUnderline = true;
			this.buttonAddBranch.Label = "gtk-new";
			this.vbox3.Add(this.buttonAddBranch);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.buttonAddBranch]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.buttonEditBranch = new global::Gtk.Button();
			this.buttonEditBranch.CanFocus = true;
			this.buttonEditBranch.Name = "buttonEditBranch";
			this.buttonEditBranch.UseStock = true;
			this.buttonEditBranch.UseUnderline = true;
			this.buttonEditBranch.Label = "gtk-edit";
			this.vbox3.Add(this.buttonEditBranch);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.buttonEditBranch]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.buttonRemoveBranch = new global::Gtk.Button();
			this.buttonRemoveBranch.CanFocus = true;
			this.buttonRemoveBranch.Name = "buttonRemoveBranch";
			this.buttonRemoveBranch.UseStock = true;
			this.buttonRemoveBranch.UseUnderline = true;
			this.buttonRemoveBranch.Label = "gtk-delete";
			this.vbox3.Add(this.buttonRemoveBranch);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.buttonRemoveBranch]));
			w6.Position = 2;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.buttonSetDefaultBranch = new global::Gtk.Button();
			this.buttonSetDefaultBranch.CanFocus = true;
			this.buttonSetDefaultBranch.Name = "buttonSetDefaultBranch";
			this.buttonSetDefaultBranch.UseUnderline = true;
			this.buttonSetDefaultBranch.Label = global::Mono.Unix.Catalog.GetString("Switch to Branch");
			this.vbox3.Add(this.buttonSetDefaultBranch);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.buttonSetDefaultBranch]));
			w7.Position = 3;
			w7.Expand = false;
			w7.Fill = false;
			this.hbox1.Add(this.vbox3);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox3]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w9.Position = 0;
			this.notebook1.Add(this.vbox2);
			// Notebook tab
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Branches");
			this.notebook1.SetTabLabel(this.vbox2, this.label1);
			this.label1.ShowAll();
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.vbox6 = new global::Gtk.VBox();
			this.vbox6.Name = "vbox6";
			this.vbox6.Spacing = 6;
			this.vbox6.BorderWidth = ((uint)(9));
			// Container child vbox6.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.GtkScrolledWindow2 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow2.Name = "GtkScrolledWindow2";
			this.GtkScrolledWindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow2.Gtk.Container+ContainerChild
			this.listTags = new global::Gtk.TreeView();
			this.listTags.CanFocus = true;
			this.listTags.Name = "listTags";
			this.GtkScrolledWindow2.Add(this.listTags);
			this.hbox3.Add(this.GtkScrolledWindow2);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.GtkScrolledWindow2]));
			w12.Position = 0;
			// Container child hbox3.Gtk.Box+BoxChild
			this.vbox7 = new global::Gtk.VBox();
			this.vbox7.Name = "vbox7";
			this.vbox7.Spacing = 6;
			// Container child vbox7.Gtk.Box+BoxChild
			this.buttonAddTag = new global::Gtk.Button();
			this.buttonAddTag.CanFocus = true;
			this.buttonAddTag.Name = "buttonAddTag";
			this.buttonAddTag.UseStock = true;
			this.buttonAddTag.UseUnderline = true;
			this.buttonAddTag.Label = "gtk-new";
			this.vbox7.Add(this.buttonAddTag);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.buttonAddTag]));
			w13.Position = 0;
			w13.Expand = false;
			w13.Fill = false;
			// Container child vbox7.Gtk.Box+BoxChild
			this.buttonPushTag = new global::Gtk.Button();
			this.buttonPushTag.CanFocus = true;
			this.buttonPushTag.Name = "buttonPushTag";
			this.buttonPushTag.UseUnderline = true;
			this.buttonPushTag.Label = global::Mono.Unix.Catalog.GetString("_Push");
			this.vbox7.Add(this.buttonPushTag);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.buttonPushTag]));
			w14.Position = 1;
			w14.Expand = false;
			w14.Fill = false;
			// Container child vbox7.Gtk.Box+BoxChild
			this.buttonRemoveTag = new global::Gtk.Button();
			this.buttonRemoveTag.CanFocus = true;
			this.buttonRemoveTag.Name = "buttonRemoveTag";
			this.buttonRemoveTag.UseStock = true;
			this.buttonRemoveTag.UseUnderline = true;
			this.buttonRemoveTag.Label = "gtk-delete";
			this.vbox7.Add(this.buttonRemoveTag);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox7[this.buttonRemoveTag]));
			w15.Position = 2;
			w15.Expand = false;
			w15.Fill = false;
			this.hbox3.Add(this.vbox7);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.vbox7]));
			w16.Position = 1;
			w16.Expand = false;
			w16.Fill = false;
			this.vbox6.Add(this.hbox3);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox6[this.hbox3]));
			w17.Position = 0;
			this.notebook1.Add(this.vbox6);
			global::Gtk.Notebook.NotebookChild w18 = ((global::Gtk.Notebook.NotebookChild)(this.notebook1[this.vbox6]));
			w18.Position = 1;
			// Notebook tab
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("Tags");
			this.notebook1.SetTabLabel(this.vbox6, this.label5);
			this.label5.ShowAll();
			// Container child notebook1.Gtk.Notebook+NotebookChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			this.vbox4.BorderWidth = ((uint)(9));
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.GtkScrolledWindow1 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow1.Name = "GtkScrolledWindow1";
			this.GtkScrolledWindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow1.Gtk.Container+ContainerChild
			this.treeRemotes = new global::Gtk.TreeView();
			this.treeRemotes.CanFocus = true;
			this.treeRemotes.Name = "treeRemotes";
			this.GtkScrolledWindow1.Add(this.treeRemotes);
			this.hbox2.Add(this.GtkScrolledWindow1);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.GtkScrolledWindow1]));
			w20.Position = 0;
			// Container child hbox2.Gtk.Box+BoxChild
			this.vbox5 = new global::Gtk.VBox();
			this.vbox5.Name = "vbox5";
			this.vbox5.Spacing = 6;
			// Container child vbox5.Gtk.Box+BoxChild
			this.buttonAddRemote = new global::Gtk.Button();
			this.buttonAddRemote.CanFocus = true;
			this.buttonAddRemote.Name = "buttonAddRemote";
			this.buttonAddRemote.UseStock = true;
			this.buttonAddRemote.UseUnderline = true;
			this.buttonAddRemote.Label = "gtk-add";
			this.vbox5.Add(this.buttonAddRemote);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.buttonAddRemote]));
			w21.Position = 0;
			w21.Expand = false;
			w21.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.buttonEditRemote = new global::Gtk.Button();
			this.buttonEditRemote.CanFocus = true;
			this.buttonEditRemote.Name = "buttonEditRemote";
			this.buttonEditRemote.UseStock = true;
			this.buttonEditRemote.UseUnderline = true;
			this.buttonEditRemote.Label = "gtk-edit";
			this.vbox5.Add(this.buttonEditRemote);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.buttonEditRemote]));
			w22.Position = 1;
			w22.Expand = false;
			w22.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.buttonRemoveRemote = new global::Gtk.Button();
			this.buttonRemoveRemote.CanFocus = true;
			this.buttonRemoveRemote.Name = "buttonRemoveRemote";
			this.buttonRemoveRemote.UseStock = true;
			this.buttonRemoveRemote.UseUnderline = true;
			this.buttonRemoveRemote.Label = "gtk-remove";
			this.vbox5.Add(this.buttonRemoveRemote);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.buttonRemoveRemote]));
			w23.Position = 2;
			w23.Expand = false;
			w23.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.hseparator2 = new global::Gtk.HSeparator();
			this.hseparator2.Name = "hseparator2";
			this.vbox5.Add(this.hseparator2);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.hseparator2]));
			w24.Position = 3;
			w24.Expand = false;
			w24.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.buttonTrackRemote = new global::Gtk.Button();
			this.buttonTrackRemote.CanFocus = true;
			this.buttonTrackRemote.Name = "buttonTrackRemote";
			this.buttonTrackRemote.UseUnderline = true;
			this.buttonTrackRemote.Label = global::Mono.Unix.Catalog.GetString("Track in Local Branch");
			this.vbox5.Add(this.buttonTrackRemote);
			global::Gtk.Box.BoxChild w25 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.buttonTrackRemote]));
			w25.Position = 4;
			w25.Expand = false;
			w25.Fill = false;
			// Container child vbox5.Gtk.Box+BoxChild
			this.buttonFetch = new global::Gtk.Button();
			this.buttonFetch.CanFocus = true;
			this.buttonFetch.Name = "buttonFetch";
			this.buttonFetch.UseUnderline = true;
			this.buttonFetch.Label = global::Mono.Unix.Catalog.GetString("Fetch");
			this.vbox5.Add(this.buttonFetch);
			global::Gtk.Box.BoxChild w26 = ((global::Gtk.Box.BoxChild)(this.vbox5[this.buttonFetch]));
			w26.Position = 5;
			w26.Expand = false;
			w26.Fill = false;
			this.hbox2.Add(this.vbox5);
			global::Gtk.Box.BoxChild w27 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.vbox5]));
			w27.Position = 1;
			w27.Expand = false;
			w27.Fill = false;
			this.vbox4.Add(this.hbox2);
			global::Gtk.Box.BoxChild w28 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox2]));
			w28.Position = 0;
			this.notebook1.Add(this.vbox4);
			global::Gtk.Notebook.NotebookChild w29 = ((global::Gtk.Notebook.NotebookChild)(this.notebook1[this.vbox4]));
			w29.Position = 2;
			// Notebook tab
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Remote Sources");
			this.notebook1.SetTabLabel(this.vbox4, this.label2);
			this.label2.ShowAll();
			w1.Add(this.notebook1);
			global::Gtk.Box.BoxChild w30 = ((global::Gtk.Box.BoxChild)(w1[this.notebook1]));
			w30.Position = 0;
			// Internal child MonoDevelop.VersionControl.Git.GitConfigurationDialog.ActionArea
			global::Gtk.HButtonBox w31 = this.ActionArea;
			w31.Name = "dialog1_ActionArea";
			w31.Spacing = 10;
			w31.BorderWidth = ((uint)(5));
			w31.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-close";
			this.AddActionWidget(this.buttonOk, -7);
			global::Gtk.ButtonBox.ButtonBoxChild w32 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w31[this.buttonOk]));
			w32.Expand = false;
			w32.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 602;
			this.DefaultHeight = 410;
			this.Hide();
			this.buttonAddBranch.Clicked += new global::System.EventHandler(this.OnButtonAddBranchClicked);
			this.buttonEditBranch.Clicked += new global::System.EventHandler(this.OnButtonEditBranchClicked);
			this.buttonRemoveBranch.Clicked += new global::System.EventHandler(this.OnButtonRemoveBranchClicked);
			this.buttonSetDefaultBranch.Clicked += new global::System.EventHandler(this.OnButtonSetDefaultBranchClicked);
			this.buttonAddTag.Clicked += new global::System.EventHandler(this.OnButtonNewTagClicked);
			this.buttonPushTag.Clicked += new global::System.EventHandler(this.OnButtonPushTagClicked);
			this.buttonRemoveTag.Clicked += new global::System.EventHandler(this.OnButtonRemoveTagClicked);
			this.buttonAddRemote.Clicked += new global::System.EventHandler(this.OnButtonAddRemoteClicked);
			this.buttonEditRemote.Clicked += new global::System.EventHandler(this.OnButtonEditRemoteClicked);
			this.buttonRemoveRemote.Clicked += new global::System.EventHandler(this.OnButtonRemoveRemoteClicked);
			this.buttonTrackRemote.Clicked += new global::System.EventHandler(this.OnButtonTrackRemoteClicked);
			this.buttonFetch.Clicked += new global::System.EventHandler(this.OnButtonFetchClicked);
		}
	}
}
#pragma warning restore 436
