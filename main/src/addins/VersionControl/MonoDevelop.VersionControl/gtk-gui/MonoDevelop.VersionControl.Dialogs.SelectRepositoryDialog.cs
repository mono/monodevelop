#pragma warning disable 436

namespace MonoDevelop.VersionControl.Dialogs
{
	internal partial class SelectRepositoryDialog
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Notebook notebook;

		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label3;

		private global::Gtk.ComboBox repCombo;

		private global::Gtk.HSeparator hseparator1;

		private global::Gtk.EventBox repoContainer;

		private global::Gtk.Label label1;

		private global::Gtk.HBox hbox2;

		private global::Gtk.ScrolledWindow scrolledwindow2;

		private global::Gtk.TreeView repoTree;

		private global::Gtk.VButtonBox vbuttonbox1;

		private global::Gtk.Button buttonAdd;

		private global::Gtk.Button buttonRemove;

		private global::Gtk.Button buttonEdit;

		private global::Gtk.Label label2;

		private global::Gtk.Table table1;

		private global::Gtk.HBox boxFolder;

		private global::Gtk.Entry entryFolder;

		private global::Gtk.Button buttonBrowse;

		private global::Gtk.HBox boxMessage;

		private global::Gtk.Entry entryMessage;

		private global::Gtk.Entry entryName;

		private global::Gtk.Label label5;

		private global::Gtk.Label labelMessage;

		private global::Gtk.Label labelName;

		private global::Gtk.Label labelRepository;

		private global::Gtk.Label labelTargetDir;

		private global::Gtk.Button button559;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Select Repository");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			// Internal child MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Events = ((global::Gdk.EventMask)(256));
			w1.Name = "dialog_VBox";
			w1.Spacing = 6;
			// Container child dialog_VBox.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 12;
			this.vbox1.BorderWidth = ((uint)(6));
			// Container child vbox1.Gtk.Box+BoxChild
			this.notebook = new global::Gtk.Notebook();
			this.notebook.CanFocus = true;
			this.notebook.Name = "notebook";
			this.notebook.CurrentPage = 0;
			// Container child notebook.Gtk.Notebook+NotebookChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Type:");
			this.hbox1.Add(this.label3);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label3]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.repCombo = global::Gtk.ComboBox.NewText();
			this.repCombo.Name = "repCombo";
			this.hbox1.Add(this.repCombo);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.repCombo]));
			w3.Position = 1;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hseparator1 = new global::Gtk.HSeparator();
			this.hseparator1.Name = "hseparator1";
			this.vbox2.Add(this.hseparator1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hseparator1]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.repoContainer = new global::Gtk.EventBox();
			this.repoContainer.Name = "repoContainer";
			this.vbox2.Add(this.repoContainer);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.repoContainer]));
			w6.Position = 2;
			this.notebook.Add(this.vbox2);
			// Notebook tab
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Connect to Repository");
			this.notebook.SetTabLabel(this.vbox2, this.label1);
			this.label1.ShowAll();
			// Container child notebook.Gtk.Notebook+NotebookChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			this.hbox2.BorderWidth = ((uint)(6));
			// Container child hbox2.Gtk.Box+BoxChild
			this.scrolledwindow2 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow2.CanFocus = true;
			this.scrolledwindow2.Name = "scrolledwindow2";
			this.scrolledwindow2.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow2.Gtk.Container+ContainerChild
			this.repoTree = new global::Gtk.TreeView();
			this.repoTree.CanFocus = true;
			this.repoTree.Name = "repoTree";
			this.scrolledwindow2.Add(this.repoTree);
			this.hbox2.Add(this.scrolledwindow2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.scrolledwindow2]));
			w9.Position = 0;
			// Container child hbox2.Gtk.Box+BoxChild
			this.vbuttonbox1 = new global::Gtk.VButtonBox();
			this.vbuttonbox1.Name = "vbuttonbox1";
			this.vbuttonbox1.Spacing = 6;
			this.vbuttonbox1.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(3));
			// Container child vbuttonbox1.Gtk.ButtonBox+ButtonBoxChild
			this.buttonAdd = new global::Gtk.Button();
			this.buttonAdd.CanFocus = true;
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.UseStock = true;
			this.buttonAdd.UseUnderline = true;
			this.buttonAdd.Label = "gtk-add";
			this.vbuttonbox1.Add(this.buttonAdd);
			global::Gtk.ButtonBox.ButtonBoxChild w10 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.vbuttonbox1[this.buttonAdd]));
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbuttonbox1.Gtk.ButtonBox+ButtonBoxChild
			this.buttonRemove = new global::Gtk.Button();
			this.buttonRemove.CanFocus = true;
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.UseStock = true;
			this.buttonRemove.UseUnderline = true;
			this.buttonRemove.Label = "gtk-remove";
			this.vbuttonbox1.Add(this.buttonRemove);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.vbuttonbox1[this.buttonRemove]));
			w11.Position = 1;
			w11.Expand = false;
			w11.Fill = false;
			// Container child vbuttonbox1.Gtk.ButtonBox+ButtonBoxChild
			this.buttonEdit = new global::Gtk.Button();
			this.buttonEdit.CanFocus = true;
			this.buttonEdit.Name = "buttonEdit";
			this.buttonEdit.UseStock = true;
			this.buttonEdit.UseUnderline = true;
			this.buttonEdit.Label = "gtk-edit";
			this.vbuttonbox1.Add(this.buttonEdit);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(this.vbuttonbox1[this.buttonEdit]));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			this.hbox2.Add(this.vbuttonbox1);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.vbuttonbox1]));
			w13.Position = 1;
			w13.Expand = false;
			w13.Fill = false;
			this.notebook.Add(this.hbox2);
			global::Gtk.Notebook.NotebookChild w14 = ((global::Gtk.Notebook.NotebookChild)(this.notebook[this.hbox2]));
			w14.Position = 1;
			// Notebook tab
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Registered Repositories");
			this.notebook.SetTabLabel(this.hbox2, this.label2);
			this.label2.ShowAll();
			this.vbox1.Add(this.notebook);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.notebook]));
			w15.Position = 0;
			// Container child vbox1.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table(((uint)(4)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.boxFolder = new global::Gtk.HBox();
			this.boxFolder.Name = "boxFolder";
			this.boxFolder.Spacing = 6;
			// Container child boxFolder.Gtk.Box+BoxChild
			this.entryFolder = new global::Gtk.Entry();
			this.entryFolder.CanFocus = true;
			this.entryFolder.Name = "entryFolder";
			this.entryFolder.IsEditable = true;
			this.entryFolder.InvisibleChar = '●';
			this.boxFolder.Add(this.entryFolder);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.boxFolder[this.entryFolder]));
			w16.Position = 0;
			// Container child boxFolder.Gtk.Box+BoxChild
			this.buttonBrowse = new global::Gtk.Button();
			this.buttonBrowse.CanFocus = true;
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Label = global::Mono.Unix.Catalog.GetString("Browse...");
			this.boxFolder.Add(this.buttonBrowse);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.boxFolder[this.buttonBrowse]));
			w17.Position = 1;
			w17.Expand = false;
			w17.Fill = false;
			this.table1.Add(this.boxFolder);
			global::Gtk.Table.TableChild w18 = ((global::Gtk.Table.TableChild)(this.table1[this.boxFolder]));
			w18.TopAttach = ((uint)(1));
			w18.BottomAttach = ((uint)(2));
			w18.LeftAttach = ((uint)(1));
			w18.RightAttach = ((uint)(2));
			w18.XOptions = ((global::Gtk.AttachOptions)(4));
			w18.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.boxMessage = new global::Gtk.HBox();
			this.boxMessage.Name = "boxMessage";
			this.boxMessage.Spacing = 6;
			// Container child boxMessage.Gtk.Box+BoxChild
			this.entryMessage = new global::Gtk.Entry();
			this.entryMessage.CanFocus = true;
			this.entryMessage.Name = "entryMessage";
			this.entryMessage.IsEditable = true;
			this.entryMessage.InvisibleChar = '●';
			this.boxMessage.Add(this.entryMessage);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.boxMessage[this.entryMessage]));
			w19.Position = 0;
			this.table1.Add(this.boxMessage);
			global::Gtk.Table.TableChild w20 = ((global::Gtk.Table.TableChild)(this.table1[this.boxMessage]));
			w20.TopAttach = ((uint)(3));
			w20.BottomAttach = ((uint)(4));
			w20.LeftAttach = ((uint)(1));
			w20.RightAttach = ((uint)(2));
			w20.XOptions = ((global::Gtk.AttachOptions)(4));
			w20.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryName = new global::Gtk.Entry();
			this.entryName.CanFocus = true;
			this.entryName.Name = "entryName";
			this.entryName.IsEditable = true;
			this.entryName.InvisibleChar = '●';
			this.table1.Add(this.entryName);
			global::Gtk.Table.TableChild w21 = ((global::Gtk.Table.TableChild)(this.table1[this.entryName]));
			w21.TopAttach = ((uint)(2));
			w21.BottomAttach = ((uint)(3));
			w21.LeftAttach = ((uint)(1));
			w21.RightAttach = ((uint)(2));
			w21.XOptions = ((global::Gtk.AttachOptions)(4));
			w21.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.Xalign = 0F;
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("Repository:");
			this.table1.Add(this.label5);
			global::Gtk.Table.TableChild w22 = ((global::Gtk.Table.TableChild)(this.table1[this.label5]));
			w22.XOptions = ((global::Gtk.AttachOptions)(4));
			w22.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.labelMessage = new global::Gtk.Label();
			this.labelMessage.Name = "labelMessage";
			this.labelMessage.Xalign = 0F;
			this.labelMessage.LabelProp = global::Mono.Unix.Catalog.GetString("Message:");
			this.table1.Add(this.labelMessage);
			global::Gtk.Table.TableChild w23 = ((global::Gtk.Table.TableChild)(this.table1[this.labelMessage]));
			w23.TopAttach = ((uint)(3));
			w23.BottomAttach = ((uint)(4));
			w23.XOptions = ((global::Gtk.AttachOptions)(4));
			w23.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.labelName = new global::Gtk.Label();
			this.labelName.Name = "labelName";
			this.labelName.Xalign = 0F;
			this.labelName.LabelProp = global::Mono.Unix.Catalog.GetString("Module name:");
			this.table1.Add(this.labelName);
			global::Gtk.Table.TableChild w24 = ((global::Gtk.Table.TableChild)(this.table1[this.labelName]));
			w24.TopAttach = ((uint)(2));
			w24.BottomAttach = ((uint)(3));
			w24.XOptions = ((global::Gtk.AttachOptions)(4));
			w24.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.labelRepository = new global::Gtk.Label();
			this.labelRepository.Name = "labelRepository";
			this.labelRepository.Xalign = 0F;
			this.table1.Add(this.labelRepository);
			global::Gtk.Table.TableChild w25 = ((global::Gtk.Table.TableChild)(this.table1[this.labelRepository]));
			w25.LeftAttach = ((uint)(1));
			w25.RightAttach = ((uint)(2));
			w25.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.labelTargetDir = new global::Gtk.Label();
			this.labelTargetDir.Name = "labelTargetDir";
			this.labelTargetDir.Xalign = 0F;
			this.labelTargetDir.LabelProp = global::Mono.Unix.Catalog.GetString("Target directory:");
			this.table1.Add(this.labelTargetDir);
			global::Gtk.Table.TableChild w26 = ((global::Gtk.Table.TableChild)(this.table1[this.labelTargetDir]));
			w26.TopAttach = ((uint)(1));
			w26.BottomAttach = ((uint)(2));
			w26.XOptions = ((global::Gtk.AttachOptions)(4));
			w26.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox1.Add(this.table1);
			global::Gtk.Box.BoxChild w27 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.table1]));
			w27.Position = 1;
			w27.Expand = false;
			w27.Fill = false;
			w1.Add(this.vbox1);
			global::Gtk.Box.BoxChild w28 = ((global::Gtk.Box.BoxChild)(w1[this.vbox1]));
			w28.Position = 0;
			// Internal child MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog.ActionArea
			global::Gtk.HButtonBox w29 = this.ActionArea;
			w29.Events = ((global::Gdk.EventMask)(256));
			w29.Name = "VersionControlAddIn.SelectRepositoryDialog_ActionArea";
			w29.Spacing = 10;
			w29.BorderWidth = ((uint)(5));
			w29.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child VersionControlAddIn.SelectRepositoryDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button559 = new global::Gtk.Button();
			this.button559.CanDefault = true;
			this.button559.CanFocus = true;
			this.button559.Name = "button559";
			this.button559.UseStock = true;
			this.button559.UseUnderline = true;
			this.button559.Label = "gtk-cancel";
			this.AddActionWidget(this.button559, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w30 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w29[this.button559]));
			w30.Expand = false;
			w30.Fill = false;
			// Container child VersionControlAddIn.SelectRepositoryDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			w29.Add(this.buttonOk);
			global::Gtk.ButtonBox.ButtonBoxChild w31 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w29[this.buttonOk]));
			w31.Position = 1;
			w31.Expand = false;
			w31.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 617;
			this.DefaultHeight = 438;
			this.Hide();
			this.notebook.ChangeCurrentPage += new global::Gtk.ChangeCurrentPageHandler(this.OnNotebookChangeCurrentPage);
			this.repCombo.Changed += new global::System.EventHandler(this.OnRepComboChanged);
			this.repoTree.CursorChanged += new global::System.EventHandler(this.OnRepoTreeCursorChanged);
			this.buttonAdd.Clicked += new global::System.EventHandler(this.OnButtonAddClicked);
			this.buttonRemove.Clicked += new global::System.EventHandler(this.OnButtonRemoveClicked);
			this.buttonEdit.Clicked += new global::System.EventHandler(this.OnButtonEditClicked);
			this.entryFolder.Changed += new global::System.EventHandler(this.OnEntryFolderChanged);
			this.buttonBrowse.Clicked += new global::System.EventHandler(this.OnButtonBrowseClicked);
			this.buttonOk.Clicked += new global::System.EventHandler(this.OnButtonOkClicked);
		}
	}
}
#pragma warning restore 436
