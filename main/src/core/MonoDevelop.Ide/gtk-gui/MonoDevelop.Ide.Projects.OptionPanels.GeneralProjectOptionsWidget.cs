#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class GeneralProjectOptionsWidget
	{
		private global::Gtk.VBox vbox40;

		private global::Gtk.VBox vbox47;

		private global::Gtk.Label informationHeaderLabel;

		private global::Gtk.HBox hbox29;

		private global::Gtk.Label label55;

		private global::Gtk.VBox vbox46;

		private global::Gtk.Table table11;

		private global::Gtk.Label defaultNamespaceLabel;

		private global::Gtk.Label descriptionLabel;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Entry entryVersion;

		private global::Gtk.CheckButton checkSolutionVersion;

		private global::Gtk.Label label1;

		private global::Gtk.Label nameLabel;

		private global::Gtk.Entry projectDefaultNamespaceEntry;

		private global::Gtk.Entry projectNameEntry;

		private global::Gtk.ScrolledWindow scrolledwindow5;

		private global::Gtk.TextView projectDescriptionTextView;

		private global::Gtk.HBox msbuildOptionsSection;

		private global::Gtk.Label label51;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.GeneralProjectOptionsWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.GeneralProjectOptionsWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.GeneralProjectOptionsWidget.Gtk.Container+ContainerChild
			this.vbox40 = new global::Gtk.VBox();
			this.vbox40.Name = "vbox40";
			this.vbox40.Spacing = 12;
			// Container child vbox40.Gtk.Box+BoxChild
			this.vbox47 = new global::Gtk.VBox();
			this.vbox47.Name = "vbox47";
			this.vbox47.Spacing = 6;
			// Container child vbox47.Gtk.Box+BoxChild
			this.informationHeaderLabel = new global::Gtk.Label();
			this.informationHeaderLabel.Name = "informationHeaderLabel";
			this.informationHeaderLabel.Xalign = 0F;
			this.informationHeaderLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Project Information</b>");
			this.informationHeaderLabel.UseMarkup = true;
			this.informationHeaderLabel.UseUnderline = true;
			this.vbox47.Add(this.informationHeaderLabel);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox47[this.informationHeaderLabel]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox47.Gtk.Box+BoxChild
			this.hbox29 = new global::Gtk.HBox();
			this.hbox29.Name = "hbox29";
			// Container child hbox29.Gtk.Box+BoxChild
			this.label55 = new global::Gtk.Label();
			this.label55.WidthRequest = 18;
			this.label55.Name = "label55";
			this.hbox29.Add(this.label55);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox29[this.label55]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox29.Gtk.Box+BoxChild
			this.vbox46 = new global::Gtk.VBox();
			this.vbox46.Name = "vbox46";
			this.vbox46.Spacing = 6;
			// Container child vbox46.Gtk.Box+BoxChild
			this.table11 = new global::Gtk.Table(((uint)(4)), ((uint)(2)), false);
			this.table11.Name = "table11";
			this.table11.RowSpacing = ((uint)(6));
			this.table11.ColumnSpacing = ((uint)(6));
			// Container child table11.Gtk.Table+TableChild
			this.defaultNamespaceLabel = new global::Gtk.Label();
			this.defaultNamespaceLabel.Name = "defaultNamespaceLabel";
			this.defaultNamespaceLabel.Xalign = 0F;
			this.defaultNamespaceLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Default Namespace:");
			this.table11.Add(this.defaultNamespaceLabel);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table11[this.defaultNamespaceLabel]));
			w3.TopAttach = ((uint)(3));
			w3.BottomAttach = ((uint)(4));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table11.Gtk.Table+TableChild
			this.descriptionLabel = new global::Gtk.Label();
			this.descriptionLabel.Name = "descriptionLabel";
			this.descriptionLabel.Xalign = 0F;
			this.descriptionLabel.Yalign = 0F;
			this.descriptionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("_Description:");
			this.descriptionLabel.UseUnderline = true;
			this.table11.Add(this.descriptionLabel);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table11[this.descriptionLabel]));
			w4.TopAttach = ((uint)(2));
			w4.BottomAttach = ((uint)(3));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table11.Gtk.Table+TableChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.entryVersion = new global::Gtk.Entry();
			this.entryVersion.CanFocus = true;
			this.entryVersion.Name = "entryVersion";
			this.entryVersion.IsEditable = true;
			this.entryVersion.InvisibleChar = '●';
			this.hbox1.Add(this.entryVersion);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.entryVersion]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.checkSolutionVersion = new global::Gtk.CheckButton();
			this.checkSolutionVersion.CanFocus = true;
			this.checkSolutionVersion.Name = "checkSolutionVersion";
			this.checkSolutionVersion.Label = global::Mono.Unix.Catalog.GetString("Get version from parent solution");
			this.checkSolutionVersion.DrawIndicator = true;
			this.checkSolutionVersion.UseUnderline = true;
			this.hbox1.Add(this.checkSolutionVersion);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.checkSolutionVersion]));
			w6.Position = 1;
			this.table11.Add(this.hbox1);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table11[this.hbox1]));
			w7.TopAttach = ((uint)(1));
			w7.BottomAttach = ((uint)(2));
			w7.LeftAttach = ((uint)(1));
			w7.RightAttach = ((uint)(2));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table11.Gtk.Table+TableChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Version:");
			this.table11.Add(this.label1);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table11[this.label1]));
			w8.TopAttach = ((uint)(1));
			w8.BottomAttach = ((uint)(2));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table11.Gtk.Table+TableChild
			this.nameLabel = new global::Gtk.Label();
			this.nameLabel.Name = "nameLabel";
			this.nameLabel.Xalign = 0F;
			this.nameLabel.LabelProp = global::Mono.Unix.Catalog.GetString("_Name:");
			this.nameLabel.UseUnderline = true;
			this.table11.Add(this.nameLabel);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.table11[this.nameLabel]));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table11.Gtk.Table+TableChild
			this.projectDefaultNamespaceEntry = new global::Gtk.Entry();
			this.projectDefaultNamespaceEntry.Name = "projectDefaultNamespaceEntry";
			this.projectDefaultNamespaceEntry.IsEditable = true;
			this.projectDefaultNamespaceEntry.InvisibleChar = '●';
			this.table11.Add(this.projectDefaultNamespaceEntry);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table11[this.projectDefaultNamespaceEntry]));
			w10.TopAttach = ((uint)(3));
			w10.BottomAttach = ((uint)(4));
			w10.LeftAttach = ((uint)(1));
			w10.RightAttach = ((uint)(2));
			w10.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table11.Gtk.Table+TableChild
			this.projectNameEntry = new global::Gtk.Entry();
			this.projectNameEntry.Name = "projectNameEntry";
			this.projectNameEntry.IsEditable = true;
			this.projectNameEntry.InvisibleChar = '●';
			this.table11.Add(this.projectNameEntry);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.table11[this.projectNameEntry]));
			w11.LeftAttach = ((uint)(1));
			w11.RightAttach = ((uint)(2));
			w11.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table11.Gtk.Table+TableChild
			this.scrolledwindow5 = new global::Gtk.ScrolledWindow();
			this.scrolledwindow5.WidthRequest = 350;
			this.scrolledwindow5.HeightRequest = 100;
			this.scrolledwindow5.Name = "scrolledwindow5";
			this.scrolledwindow5.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindow5.Gtk.Container+ContainerChild
			this.projectDescriptionTextView = new global::Gtk.TextView();
			this.projectDescriptionTextView.Name = "projectDescriptionTextView";
			this.scrolledwindow5.Add(this.projectDescriptionTextView);
			this.table11.Add(this.scrolledwindow5);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.table11[this.scrolledwindow5]));
			w13.TopAttach = ((uint)(2));
			w13.BottomAttach = ((uint)(3));
			w13.LeftAttach = ((uint)(1));
			w13.RightAttach = ((uint)(2));
			this.vbox46.Add(this.table11);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox46[this.table11]));
			w14.Position = 0;
			this.hbox29.Add(this.vbox46);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.hbox29[this.vbox46]));
			w15.Position = 1;
			this.vbox47.Add(this.hbox29);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.vbox47[this.hbox29]));
			w16.Position = 1;
			w16.Expand = false;
			w16.Fill = false;
			this.vbox40.Add(this.vbox47);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox40[this.vbox47]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			// Container child vbox40.Gtk.Box+BoxChild
			this.msbuildOptionsSection = new global::Gtk.HBox();
			this.msbuildOptionsSection.Name = "msbuildOptionsSection";
			// Container child msbuildOptionsSection.Gtk.Box+BoxChild
			this.label51 = new global::Gtk.Label();
			this.label51.WidthRequest = 18;
			this.label51.Name = "label51";
			this.msbuildOptionsSection.Add(this.label51);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.msbuildOptionsSection[this.label51]));
			w18.Position = 0;
			w18.Expand = false;
			w18.Fill = false;
			this.vbox40.Add(this.msbuildOptionsSection);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.vbox40[this.msbuildOptionsSection]));
			w19.Position = 1;
			w19.Expand = false;
			w19.Fill = false;
			this.Add(this.vbox40);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.descriptionLabel.MnemonicWidget = this.scrolledwindow5;
			this.nameLabel.MnemonicWidget = this.projectNameEntry;
			this.Show();
			this.checkSolutionVersion.Clicked += new global::System.EventHandler(this.OnCheckSolutionVersionClicked);
		}
	}
}
#pragma warning restore 436
