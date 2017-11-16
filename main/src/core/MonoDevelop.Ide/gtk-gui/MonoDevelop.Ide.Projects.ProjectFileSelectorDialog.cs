#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	public partial class ProjectFileSelectorDialog
	{
		private global::Gtk.HPaned hpaned1;

		private global::Gtk.VBox vbox3;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView projectTree;

		private global::Gtk.VBox vbox4;

		private global::Gtk.ScrolledWindow GtkScrolledWindow1;

		private global::Gtk.TreeView fileList;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Button AddFileButton;

		private global::MonoDevelop.Components.ImageView imageAdd;

		private global::Gtk.HBox typeBox;

		private global::Gtk.Label label2;

		private global::Gtk.ComboBox fileTypeCombo;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.ProjectFileSelectorDialog
			this.Name = "MonoDevelop.Ide.Projects.ProjectFileSelectorDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Select Project File...");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Internal child MonoDevelop.Ide.Projects.ProjectFileSelectorDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.hpaned1 = new global::Gtk.HPaned();
			this.hpaned1.CanFocus = true;
			this.hpaned1.Name = "hpaned1";
			this.hpaned1.Position = 182;
			this.hpaned1.BorderWidth = ((uint)(6));
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.projectTree = new global::Gtk.TreeView();
			this.projectTree.CanFocus = true;
			this.projectTree.Name = "projectTree";
			this.GtkScrolledWindow.Add(this.projectTree);
			this.vbox3.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.GtkScrolledWindow]));
			w3.Position = 0;
			this.hpaned1.Add(this.vbox3);
			global::Gtk.Paned.PanedChild w4 = ((global::Gtk.Paned.PanedChild)(this.hpaned1[this.vbox3]));
			w4.Resize = false;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.GtkScrolledWindow1 = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow1.Name = "GtkScrolledWindow1";
			this.GtkScrolledWindow1.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow1.Gtk.Container+ContainerChild
			this.fileList = new global::Gtk.TreeView();
			this.fileList.CanFocus = true;
			this.fileList.Name = "fileList";
			this.GtkScrolledWindow1.Add(this.fileList);
			this.vbox4.Add(this.GtkScrolledWindow1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.GtkScrolledWindow1]));
			w6.Position = 0;
			// Container child vbox4.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.AddFileButton = new global::Gtk.Button();
			this.AddFileButton.TooltipMarkup = "Add existing files to the project";
			this.AddFileButton.CanFocus = true;
			this.AddFileButton.Name = "AddFileButton";
			this.AddFileButton.Relief = ((global::Gtk.ReliefStyle)(2));
			// Container child AddFileButton.Gtk.Container+ContainerChild
			this.imageAdd = new global::MonoDevelop.Components.ImageView();
			this.imageAdd.Name = "imageAdd";
			this.imageAdd.IconId = "gtk-add";
			this.imageAdd.IconSize = ((global::Gtk.IconSize)(1));
			this.AddFileButton.Add(this.imageAdd);
			this.hbox2.Add(this.AddFileButton);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.AddFileButton]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			// Container child hbox2.Gtk.Box+BoxChild
			this.typeBox = new global::Gtk.HBox();
			this.typeBox.Name = "typeBox";
			this.typeBox.Spacing = 6;
			// Container child typeBox.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("File _type:");
			this.label2.UseUnderline = true;
			this.typeBox.Add(this.label2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.typeBox[this.label2]));
			w9.Position = 0;
			w9.Expand = false;
			w9.Fill = false;
			// Container child typeBox.Gtk.Box+BoxChild
			this.fileTypeCombo = global::Gtk.ComboBox.NewText();
			this.fileTypeCombo.Name = "fileTypeCombo";
			this.typeBox.Add(this.fileTypeCombo);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.typeBox[this.fileTypeCombo]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			this.hbox2.Add(this.typeBox);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.typeBox]));
			w11.PackType = ((global::Gtk.PackType)(1));
			w11.Position = 1;
			this.vbox4.Add(this.hbox2);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.hbox2]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			this.hpaned1.Add(this.vbox4);
			w1.Add(this.hpaned1);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(w1[this.hpaned1]));
			w14.Position = 0;
			// Internal child MonoDevelop.Ide.Projects.ProjectFileSelectorDialog.ActionArea
			global::Gtk.HButtonBox w15 = this.ActionArea;
			w15.Name = "dialog1_ActionArea";
			w15.Spacing = 6;
			w15.BorderWidth = ((uint)(5));
			w15.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w16 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w15[this.buttonCancel]));
			w16.Expand = false;
			w16.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w17 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w15[this.buttonOk]));
			w17.Position = 1;
			w17.Expand = false;
			w17.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 522;
			this.DefaultHeight = 416;
			this.label2.MnemonicWidget = this.fileTypeCombo;
			this.typeBox.Hide();
			this.Hide();
			this.AddFileButton.Clicked += new global::System.EventHandler(this.OnAddFileButtonClicked);
		}
	}
}
#pragma warning restore 436
