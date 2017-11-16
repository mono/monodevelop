#pragma warning disable 436

namespace MonoDevelop.WebReferences.Dialogs
{
	public partial class WCFConfigWidget
	{
		private global::Gtk.VBox dialog1_VBox;

		private global::Gtk.Table wcfOptions;

		private global::Gtk.ComboBox dictionaryCollection;

		private global::Gtk.Label label1;

		private global::Gtk.Label label2;

		private global::Gtk.Label label3;

		private global::Gtk.Label label4;

		private global::Gtk.ComboBox listAccess;

		private global::Gtk.ComboBox listAsync;

		private global::Gtk.ComboBox listCollection;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.WebReferences.Dialogs.WCFConfigWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.WebReferences.Dialogs.WCFConfigWidget";
			// Container child MonoDevelop.WebReferences.Dialogs.WCFConfigWidget.Gtk.Container+ContainerChild
			this.dialog1_VBox = new global::Gtk.VBox();
			this.dialog1_VBox.Name = "dialog1_VBox";
			this.dialog1_VBox.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.wcfOptions = new global::Gtk.Table(((uint)(7)), ((uint)(2)), false);
			this.wcfOptions.Name = "wcfOptions";
			this.wcfOptions.RowSpacing = ((uint)(6));
			this.wcfOptions.ColumnSpacing = ((uint)(6));
			// Container child wcfOptions.Gtk.Table+TableChild
			this.dictionaryCollection = global::Gtk.ComboBox.NewText();
			this.dictionaryCollection.Name = "dictionaryCollection";
			this.wcfOptions.Add(this.dictionaryCollection);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.wcfOptions[this.dictionaryCollection]));
			w1.TopAttach = ((uint)(1));
			w1.BottomAttach = ((uint)(2));
			w1.LeftAttach = ((uint)(1));
			w1.RightAttach = ((uint)(2));
			w1.XOptions = ((global::Gtk.AttachOptions)(4));
			w1.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child wcfOptions.Gtk.Table+TableChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Dictionary type:");
			this.wcfOptions.Add(this.label1);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.wcfOptions[this.label1]));
			w2.TopAttach = ((uint)(1));
			w2.BottomAttach = ((uint)(2));
			w2.XOptions = ((global::Gtk.AttachOptions)(4));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child wcfOptions.Gtk.Table+TableChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Collection Mapping:");
			this.wcfOptions.Add(this.label2);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.wcfOptions[this.label2]));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child wcfOptions.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Access level:");
			this.wcfOptions.Add(this.label3);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.wcfOptions[this.label3]));
			w4.TopAttach = ((uint)(2));
			w4.BottomAttach = ((uint)(3));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child wcfOptions.Gtk.Table+TableChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Generate Asynchronous:");
			this.wcfOptions.Add(this.label4);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.wcfOptions[this.label4]));
			w5.TopAttach = ((uint)(3));
			w5.BottomAttach = ((uint)(4));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child wcfOptions.Gtk.Table+TableChild
			this.listAccess = global::Gtk.ComboBox.NewText();
			this.listAccess.AppendText(global::Mono.Unix.Catalog.GetString("Public"));
			this.listAccess.AppendText(global::Mono.Unix.Catalog.GetString("Internal"));
			this.listAccess.Name = "listAccess";
			this.listAccess.Active = 0;
			this.wcfOptions.Add(this.listAccess);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.wcfOptions[this.listAccess]));
			w6.TopAttach = ((uint)(2));
			w6.BottomAttach = ((uint)(3));
			w6.LeftAttach = ((uint)(1));
			w6.RightAttach = ((uint)(2));
			w6.XOptions = ((global::Gtk.AttachOptions)(7));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child wcfOptions.Gtk.Table+TableChild
			this.listAsync = global::Gtk.ComboBox.NewText();
			this.listAsync.AppendText(global::Mono.Unix.Catalog.GetString("No"));
			this.listAsync.AppendText(global::Mono.Unix.Catalog.GetString("Async"));
			this.listAsync.Name = "listAsync";
			this.listAsync.Active = 0;
			this.wcfOptions.Add(this.listAsync);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.wcfOptions[this.listAsync]));
			w7.TopAttach = ((uint)(3));
			w7.BottomAttach = ((uint)(4));
			w7.LeftAttach = ((uint)(1));
			w7.RightAttach = ((uint)(2));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child wcfOptions.Gtk.Table+TableChild
			this.listCollection = global::Gtk.ComboBox.NewText();
			this.listCollection.Name = "listCollection";
			this.wcfOptions.Add(this.listCollection);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.wcfOptions[this.listCollection]));
			w8.LeftAttach = ((uint)(1));
			w8.RightAttach = ((uint)(2));
			w8.XOptions = ((global::Gtk.AttachOptions)(7));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			this.dialog1_VBox.Add(this.wcfOptions);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.dialog1_VBox[this.wcfOptions]));
			w9.Position = 0;
			this.Add(this.dialog1_VBox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
