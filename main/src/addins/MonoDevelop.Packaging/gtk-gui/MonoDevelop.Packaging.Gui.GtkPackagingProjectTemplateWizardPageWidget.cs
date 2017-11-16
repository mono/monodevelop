#pragma warning disable 436

namespace MonoDevelop.Packaging.Gui
{
	public partial class GtkPackagingProjectTemplateWizardPageWidget
	{
		private global::Gtk.HBox mainHBox;

		private global::Gtk.EventBox leftBorderEventBox;

		private global::Gtk.VBox configurationVBox;

		private global::Gtk.EventBox configurationTopEventBox;

		private global::Gtk.EventBox configurationTableEventBox;

		private global::Gtk.Table configurationTable;

		private global::Gtk.DrawingArea bottomPadding;

		private global::Gtk.EventBox idEventBox;

		private global::Gtk.EventBox organizationInfoEventBox;

		private global::Gtk.EventBox versionEventBox;

		private global::Gtk.Label packageAuthorsLabel;

		private global::Gtk.Entry packageAuthorsTextBox;

		private global::Gtk.Label packageDescriptionLabel;

		private global::Gtk.Entry packageDescriptionTextBox;

		private global::Gtk.Label packageIdLabel;

		private global::Gtk.Entry packageIdTextBox;

		private global::Gtk.DrawingArea separator;

		private global::Gtk.DrawingArea topPadding;

		private global::Gtk.EventBox configurationBottomEventBox;

		private global::Gtk.EventBox backgroundLargeImageEventBox;

		private global::Gtk.VBox backgroundLargeImageVBox;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Packaging.Gui.GtkPackagingProjectTemplateWizardPageWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Packaging.Gui.GtkPackagingProjectTemplateWizardPageWidget";
			// Container child MonoDevelop.Packaging.Gui.GtkPackagingProjectTemplateWizardPageWidget.Gtk.Container+ContainerChild
			this.mainHBox = new global::Gtk.HBox();
			this.mainHBox.Name = "mainHBox";
			// Container child mainHBox.Gtk.Box+BoxChild
			this.leftBorderEventBox = new global::Gtk.EventBox();
			this.leftBorderEventBox.WidthRequest = 30;
			this.leftBorderEventBox.Name = "leftBorderEventBox";
			this.mainHBox.Add(this.leftBorderEventBox);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.leftBorderEventBox]));
			w1.Position = 0;
			w1.Expand = false;
			// Container child mainHBox.Gtk.Box+BoxChild
			this.configurationVBox = new global::Gtk.VBox();
			this.configurationVBox.Name = "configurationVBox";
			// Container child configurationVBox.Gtk.Box+BoxChild
			this.configurationTopEventBox = new global::Gtk.EventBox();
			this.configurationTopEventBox.Name = "configurationTopEventBox";
			this.configurationVBox.Add(this.configurationTopEventBox);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.configurationVBox[this.configurationTopEventBox]));
			w2.Position = 0;
			// Container child configurationVBox.Gtk.Box+BoxChild
			this.configurationTableEventBox = new global::Gtk.EventBox();
			this.configurationTableEventBox.Name = "configurationTableEventBox";
			// Container child configurationTableEventBox.Gtk.Container+ContainerChild
			this.configurationTable = new global::Gtk.Table(((uint)(6)), ((uint)(3)), false);
			this.configurationTable.Name = "configurationTable";
			this.configurationTable.RowSpacing = ((uint)(7));
			this.configurationTable.ColumnSpacing = ((uint)(6));
			// Container child configurationTable.Gtk.Table+TableChild
			this.bottomPadding = new global::Gtk.DrawingArea();
			this.bottomPadding.WidthRequest = 132;
			this.bottomPadding.HeightRequest = 0;
			this.bottomPadding.Name = "bottomPadding";
			this.configurationTable.Add(this.bottomPadding);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.bottomPadding]));
			w3.TopAttach = ((uint)(5));
			w3.BottomAttach = ((uint)(6));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child configurationTable.Gtk.Table+TableChild
			this.idEventBox = new global::Gtk.EventBox();
			this.idEventBox.WidthRequest = 16;
			this.idEventBox.HeightRequest = 16;
			this.idEventBox.Name = "idEventBox";
			this.idEventBox.VisibleWindow = false;
			this.configurationTable.Add(this.idEventBox);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.idEventBox]));
			w4.TopAttach = ((uint)(1));
			w4.BottomAttach = ((uint)(2));
			w4.LeftAttach = ((uint)(2));
			w4.RightAttach = ((uint)(3));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.organizationInfoEventBox = new global::Gtk.EventBox();
			this.organizationInfoEventBox.WidthRequest = 16;
			this.organizationInfoEventBox.HeightRequest = 16;
			this.organizationInfoEventBox.Name = "organizationInfoEventBox";
			this.organizationInfoEventBox.VisibleWindow = false;
			// Container child organizationInfoEventBox.Gtk.Container+ContainerChild
			this.versionEventBox = new global::Gtk.EventBox();
			this.versionEventBox.WidthRequest = 16;
			this.versionEventBox.HeightRequest = 16;
			this.versionEventBox.Name = "versionEventBox";
			this.versionEventBox.VisibleWindow = false;
			this.organizationInfoEventBox.Add(this.versionEventBox);
			this.configurationTable.Add(this.organizationInfoEventBox);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.organizationInfoEventBox]));
			w6.TopAttach = ((uint)(2));
			w6.BottomAttach = ((uint)(3));
			w6.LeftAttach = ((uint)(2));
			w6.RightAttach = ((uint)(3));
			w6.XOptions = ((global::Gtk.AttachOptions)(2));
			w6.YOptions = ((global::Gtk.AttachOptions)(2));
			// Container child configurationTable.Gtk.Table+TableChild
			this.packageAuthorsLabel = new global::Gtk.Label();
			this.packageAuthorsLabel.Name = "packageAuthorsLabel";
			this.packageAuthorsLabel.Xpad = 5;
			this.packageAuthorsLabel.Xalign = 1F;
			this.packageAuthorsLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Author:");
			this.packageAuthorsLabel.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.packageAuthorsLabel);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.packageAuthorsLabel]));
			w7.TopAttach = ((uint)(3));
			w7.BottomAttach = ((uint)(4));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.packageAuthorsTextBox = new global::Gtk.Entry();
			this.packageAuthorsTextBox.CanFocus = true;
			this.packageAuthorsTextBox.Name = "packageAuthorsTextBox";
			this.packageAuthorsTextBox.IsEditable = true;
			this.packageAuthorsTextBox.InvisibleChar = '●';
			this.configurationTable.Add(this.packageAuthorsTextBox);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.packageAuthorsTextBox]));
			w8.TopAttach = ((uint)(3));
			w8.BottomAttach = ((uint)(4));
			w8.LeftAttach = ((uint)(1));
			w8.RightAttach = ((uint)(2));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.packageDescriptionLabel = new global::Gtk.Label();
			this.packageDescriptionLabel.Name = "packageDescriptionLabel";
			this.packageDescriptionLabel.Xpad = 5;
			this.packageDescriptionLabel.Xalign = 1F;
			this.packageDescriptionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Description:");
			this.packageDescriptionLabel.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.packageDescriptionLabel);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.packageDescriptionLabel]));
			w9.TopAttach = ((uint)(4));
			w9.BottomAttach = ((uint)(5));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.packageDescriptionTextBox = new global::Gtk.Entry();
			this.packageDescriptionTextBox.CanFocus = true;
			this.packageDescriptionTextBox.Name = "packageDescriptionTextBox";
			this.packageDescriptionTextBox.IsEditable = true;
			this.packageDescriptionTextBox.InvisibleChar = '●';
			this.configurationTable.Add(this.packageDescriptionTextBox);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.packageDescriptionTextBox]));
			w10.TopAttach = ((uint)(4));
			w10.BottomAttach = ((uint)(5));
			w10.LeftAttach = ((uint)(1));
			w10.RightAttach = ((uint)(2));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.packageIdLabel = new global::Gtk.Label();
			this.packageIdLabel.Name = "packageIdLabel";
			this.packageIdLabel.Xpad = 5;
			this.packageIdLabel.Xalign = 1F;
			this.packageIdLabel.LabelProp = global::Mono.Unix.Catalog.GetString("ID:");
			this.packageIdLabel.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.packageIdLabel);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.packageIdLabel]));
			w11.TopAttach = ((uint)(1));
			w11.BottomAttach = ((uint)(2));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.packageIdTextBox = new global::Gtk.Entry();
			this.packageIdTextBox.WidthRequest = 280;
			this.packageIdTextBox.CanFocus = true;
			this.packageIdTextBox.Name = "packageIdTextBox";
			this.packageIdTextBox.IsEditable = true;
			this.packageIdTextBox.InvisibleChar = '●';
			this.configurationTable.Add(this.packageIdTextBox);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.packageIdTextBox]));
			w12.TopAttach = ((uint)(1));
			w12.BottomAttach = ((uint)(2));
			w12.LeftAttach = ((uint)(1));
			w12.RightAttach = ((uint)(2));
			w12.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.separator = new global::Gtk.DrawingArea();
			this.separator.HeightRequest = 1;
			this.separator.Name = "separator";
			this.configurationTable.Add(this.separator);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.separator]));
			w13.TopAttach = ((uint)(2));
			w13.BottomAttach = ((uint)(3));
			w13.RightAttach = ((uint)(3));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			w13.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child configurationTable.Gtk.Table+TableChild
			this.topPadding = new global::Gtk.DrawingArea();
			this.topPadding.WidthRequest = 132;
			this.topPadding.HeightRequest = 0;
			this.topPadding.Name = "topPadding";
			this.configurationTable.Add(this.topPadding);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.topPadding]));
			w14.XOptions = ((global::Gtk.AttachOptions)(4));
			w14.YOptions = ((global::Gtk.AttachOptions)(0));
			this.configurationTableEventBox.Add(this.configurationTable);
			this.configurationVBox.Add(this.configurationTableEventBox);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.configurationVBox[this.configurationTableEventBox]));
			w16.Position = 1;
			w16.Expand = false;
			w16.Fill = false;
			// Container child configurationVBox.Gtk.Box+BoxChild
			this.configurationBottomEventBox = new global::Gtk.EventBox();
			this.configurationBottomEventBox.Name = "configurationBottomEventBox";
			this.configurationVBox.Add(this.configurationBottomEventBox);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.configurationVBox[this.configurationBottomEventBox]));
			w17.Position = 2;
			this.mainHBox.Add(this.configurationVBox);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.configurationVBox]));
			w18.Position = 1;
			w18.Expand = false;
			w18.Fill = false;
			// Container child mainHBox.Gtk.Box+BoxChild
			this.backgroundLargeImageEventBox = new global::Gtk.EventBox();
			this.backgroundLargeImageEventBox.Name = "backgroundLargeImageEventBox";
			// Container child backgroundLargeImageEventBox.Gtk.Container+ContainerChild
			this.backgroundLargeImageVBox = new global::Gtk.VBox();
			this.backgroundLargeImageVBox.Name = "backgroundLargeImageVBox";
			this.backgroundLargeImageEventBox.Add(this.backgroundLargeImageVBox);
			this.mainHBox.Add(this.backgroundLargeImageEventBox);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.backgroundLargeImageEventBox]));
			w20.Position = 2;
			this.Add(this.mainHBox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
