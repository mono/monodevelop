#pragma warning disable 436

namespace MonoDevelop.VersionControl.Dialogs
{
	internal partial class EditRepositoryDialog
	{
		private global::Gtk.VBox vbox1;

		private global::Gtk.Table table1;

		private global::Gtk.Entry entryName;

		private global::Gtk.Label label11;

		private global::Gtk.Label label8;

		private global::Gtk.ComboBox versionControlType;

		private global::Gtk.HSeparator hseparator2;

		private global::Gtk.EventBox repoEditorContainer;

		private global::Gtk.Button button10;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.Dialogs.EditRepositoryDialog
			this.Name = "MonoDevelop.VersionControl.Dialogs.EditRepositoryDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Repository Configuration");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.BorderWidth = ((uint)(6));
			this.DefaultWidth = 500;
			// Internal child MonoDevelop.VersionControl.Dialogs.EditRepositoryDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Events = ((global::Gdk.EventMask)(256));
			w1.Name = "dialog-vbox3";
			w1.Spacing = 6;
			// Container child dialog-vbox3.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			this.vbox1.BorderWidth = ((uint)(6));
			// Container child vbox1.Gtk.Box+BoxChild
			this.table1 = new global::Gtk.Table(((uint)(2)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.entryName = new global::Gtk.Entry();
			this.entryName.CanFocus = true;
			this.entryName.Name = "entryName";
			this.entryName.IsEditable = true;
			this.entryName.InvisibleChar = '●';
			this.table1.Add(this.entryName);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table1[this.entryName]));
			w2.TopAttach = ((uint)(1));
			w2.BottomAttach = ((uint)(2));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.label11 = new global::Gtk.Label();
			this.label11.Name = "label11";
			this.label11.Xalign = 0F;
			this.label11.LabelProp = global::Mono.Unix.Catalog.GetString("Type:");
			this.table1.Add(this.label11);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table1[this.label11]));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.Xalign = 0F;
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("Name:");
			this.table1.Add(this.label8);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1[this.label8]));
			w4.TopAttach = ((uint)(1));
			w4.BottomAttach = ((uint)(2));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.versionControlType = global::Gtk.ComboBox.NewText();
			this.versionControlType.Name = "versionControlType";
			this.table1.Add(this.versionControlType);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table1[this.versionControlType]));
			w5.LeftAttach = ((uint)(1));
			w5.RightAttach = ((uint)(2));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox1.Add(this.table1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.table1]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hseparator2 = new global::Gtk.HSeparator();
			this.hseparator2.Name = "hseparator2";
			this.vbox1.Add(this.hseparator2);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.hseparator2]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.repoEditorContainer = new global::Gtk.EventBox();
			this.repoEditorContainer.Name = "repoEditorContainer";
			this.vbox1.Add(this.repoEditorContainer);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.repoEditorContainer]));
			w8.Position = 2;
			w1.Add(this.vbox1);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(w1[this.vbox1]));
			w9.Position = 0;
			// Internal child MonoDevelop.VersionControl.Dialogs.EditRepositoryDialog.ActionArea
			global::Gtk.HButtonBox w10 = this.ActionArea;
			w10.Events = ((global::Gdk.EventMask)(256));
			w10.Name = "GtkDialog_ActionArea";
			w10.Spacing = 10;
			w10.BorderWidth = ((uint)(5));
			w10.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child GtkDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.button10 = new global::Gtk.Button();
			this.button10.CanDefault = true;
			this.button10.CanFocus = true;
			this.button10.Name = "button10";
			this.button10.UseStock = true;
			this.button10.UseUnderline = true;
			this.button10.Label = "gtk-cancel";
			this.AddActionWidget(this.button10, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w11 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10[this.button10]));
			w11.Expand = false;
			w11.Fill = false;
			// Container child GtkDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			w10.Add(this.buttonOk);
			global::Gtk.ButtonBox.ButtonBoxChild w12 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w10[this.buttonOk]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultHeight = 414;
			this.Hide();
			this.versionControlType.Changed += new global::System.EventHandler(this.OnVersionControlTypeChanged);
			this.entryName.Changed += new global::System.EventHandler(this.OnEntryNameChanged);
			this.buttonOk.Clicked += new global::System.EventHandler(this.OnButtonOkClicked);
		}
	}
}
#pragma warning restore 436
