#pragma warning disable 436

namespace MonoDevelop.Packaging.Gui
{
	public partial class GtkNuGetPackageMetadataOptionsPanelWidget
	{
		private global::Gtk.Notebook notebook;

		private global::Gtk.Table generalTable;

		private global::Gtk.Label packageAuthorsLabel;

		private global::Gtk.Entry packageAuthorsTextBox;

		private global::Gtk.VBox packageDescriptionLabelVBox;

		private global::Gtk.Label packageDescriptionLabel;

		private global::Gtk.Label packageDescriptionPaddingLabel;

		private global::Gtk.ScrolledWindow packageDescriptionScrolledWindow;

		private global::Gtk.TextView packageDescriptionTextView;

		private global::Gtk.Label packageIdLabel;

		private global::Gtk.Entry packageIdTextBox;

		private global::Gtk.Label packageVersionLabel;

		private global::Gtk.Entry packageVersionTextBox;

		private global::Gtk.Label generalTabPageLabel;

		private global::Gtk.Table detailsTable;

		private global::Gtk.Label packageCopyrightLabel;

		private global::Gtk.Entry packageCopyrightTextBox;

		private global::Gtk.CheckButton packageDevelopmentDependencyCheckBox;

		private global::Gtk.Label packageDevelopmentDependencyLabel;

		private global::Gtk.Label packageIconUrlLabel;

		private global::Gtk.Entry packageIconUrlTextBox;

		private global::Gtk.HBox packageLanguageHBox;

		private global::Gtk.ComboBoxEntry packageLanguageComboBox;

		private global::Gtk.Label packageLanguageLabel;

		private global::Gtk.Label packageLicenseUrlLabel;

		private global::Gtk.Entry packageLicenseUrlTextBox;

		private global::Gtk.Label packageOwnersLabel;

		private global::Gtk.Entry packageOwnersTextBox;

		private global::Gtk.Label packageProjectUrlLabel;

		private global::Gtk.Entry packageProjectUrlTextBox;

		private global::Gtk.VBox packageReleaseNotesLabelVBox;

		private global::Gtk.Label packageReleaseNotesLabel;

		private global::Gtk.Label packageReleaseNotesPaddingLabel;

		private global::Gtk.ScrolledWindow packageReleaseNotesScrolledWindow;

		private global::Gtk.TextView packageReleaseNotesTextView;

		private global::Gtk.CheckButton packageRequireLicenseAcceptanceCheckBox;

		private global::Gtk.Label packageRequireLicenseAcceptanceLabel;

		private global::Gtk.Label packageSummaryLabel;

		private global::Gtk.Entry packageSummaryTextBox;

		private global::Gtk.Label packageTagsLabel;

		private global::Gtk.Entry packageTagsTextBox;

		private global::Gtk.Label packageTitleLabel;

		private global::Gtk.Entry packageTitleTextBox;

		private global::Gtk.Label detailsTabPageLabel;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Packaging.Gui.GtkNuGetPackageMetadataOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Packaging.Gui.GtkNuGetPackageMetadataOptionsPanelWidget";
			// Container child MonoDevelop.Packaging.Gui.GtkNuGetPackageMetadataOptionsPanelWidget.Gtk.Container+ContainerChild
			this.notebook = new global::Gtk.Notebook();
			this.notebook.CanFocus = true;
			this.notebook.Name = "notebook";
			this.notebook.CurrentPage = 0;
			// Container child notebook.Gtk.Notebook+NotebookChild
			this.generalTable = new global::Gtk.Table(((uint)(5)), ((uint)(3)), false);
			this.generalTable.Name = "generalTable";
			this.generalTable.RowSpacing = ((uint)(6));
			this.generalTable.ColumnSpacing = ((uint)(6));
			this.generalTable.BorderWidth = ((uint)(10));
			// Container child generalTable.Gtk.Table+TableChild
			this.packageAuthorsLabel = new global::Gtk.Label();
			this.packageAuthorsLabel.Name = "packageAuthorsLabel";
			this.packageAuthorsLabel.Xalign = 0F;
			this.packageAuthorsLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Authors:");
			this.generalTable.Add(this.packageAuthorsLabel);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.generalTable[this.packageAuthorsLabel]));
			w1.TopAttach = ((uint)(2));
			w1.BottomAttach = ((uint)(3));
			w1.XOptions = ((global::Gtk.AttachOptions)(4));
			w1.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child generalTable.Gtk.Table+TableChild
			this.packageAuthorsTextBox = new global::Gtk.Entry();
			this.packageAuthorsTextBox.CanFocus = true;
			this.packageAuthorsTextBox.Name = "packageAuthorsTextBox";
			this.packageAuthorsTextBox.IsEditable = true;
			this.packageAuthorsTextBox.InvisibleChar = '●';
			this.generalTable.Add(this.packageAuthorsTextBox);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.generalTable[this.packageAuthorsTextBox]));
			w2.TopAttach = ((uint)(2));
			w2.BottomAttach = ((uint)(3));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.XOptions = ((global::Gtk.AttachOptions)(4));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child generalTable.Gtk.Table+TableChild
			this.packageDescriptionLabelVBox = new global::Gtk.VBox();
			this.packageDescriptionLabelVBox.Name = "packageDescriptionLabelVBox";
			this.packageDescriptionLabelVBox.Spacing = 6;
			// Container child packageDescriptionLabelVBox.Gtk.Box+BoxChild
			this.packageDescriptionLabel = new global::Gtk.Label();
			this.packageDescriptionLabel.WidthRequest = 164;
			this.packageDescriptionLabel.Name = "packageDescriptionLabel";
			this.packageDescriptionLabel.Xalign = 0F;
			this.packageDescriptionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Description:");
			this.packageDescriptionLabelVBox.Add(this.packageDescriptionLabel);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.packageDescriptionLabelVBox[this.packageDescriptionLabel]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			w3.Padding = ((uint)(6));
			// Container child packageDescriptionLabelVBox.Gtk.Box+BoxChild
			this.packageDescriptionPaddingLabel = new global::Gtk.Label();
			this.packageDescriptionPaddingLabel.Name = "packageDescriptionPaddingLabel";
			this.packageDescriptionPaddingLabel.Xalign = 0F;
			this.packageDescriptionLabelVBox.Add(this.packageDescriptionPaddingLabel);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.packageDescriptionLabelVBox[this.packageDescriptionPaddingLabel]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			this.generalTable.Add(this.packageDescriptionLabelVBox);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.generalTable[this.packageDescriptionLabelVBox]));
			w5.TopAttach = ((uint)(3));
			w5.BottomAttach = ((uint)(4));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child generalTable.Gtk.Table+TableChild
			this.packageDescriptionScrolledWindow = new global::Gtk.ScrolledWindow();
			this.packageDescriptionScrolledWindow.Name = "packageDescriptionScrolledWindow";
			this.packageDescriptionScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child packageDescriptionScrolledWindow.Gtk.Container+ContainerChild
			this.packageDescriptionTextView = new global::Gtk.TextView();
			this.packageDescriptionTextView.CanFocus = true;
			this.packageDescriptionTextView.Name = "packageDescriptionTextView";
			this.packageDescriptionTextView.AcceptsTab = false;
			this.packageDescriptionTextView.WrapMode = ((global::Gtk.WrapMode)(2));
			this.packageDescriptionScrolledWindow.Add(this.packageDescriptionTextView);
			this.generalTable.Add(this.packageDescriptionScrolledWindow);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.generalTable[this.packageDescriptionScrolledWindow]));
			w7.TopAttach = ((uint)(3));
			w7.BottomAttach = ((uint)(4));
			w7.LeftAttach = ((uint)(1));
			w7.RightAttach = ((uint)(2));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child generalTable.Gtk.Table+TableChild
			this.packageIdLabel = new global::Gtk.Label();
			this.packageIdLabel.Name = "packageIdLabel";
			this.packageIdLabel.Xalign = 0F;
			this.packageIdLabel.LabelProp = global::Mono.Unix.Catalog.GetString("ID:");
			this.generalTable.Add(this.packageIdLabel);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.generalTable[this.packageIdLabel]));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child generalTable.Gtk.Table+TableChild
			this.packageIdTextBox = new global::Gtk.Entry();
			this.packageIdTextBox.CanFocus = true;
			this.packageIdTextBox.Name = "packageIdTextBox";
			this.packageIdTextBox.IsEditable = true;
			this.packageIdTextBox.InvisibleChar = '●';
			this.generalTable.Add(this.packageIdTextBox);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.generalTable[this.packageIdTextBox]));
			w9.LeftAttach = ((uint)(1));
			w9.RightAttach = ((uint)(2));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child generalTable.Gtk.Table+TableChild
			this.packageVersionLabel = new global::Gtk.Label();
			this.packageVersionLabel.Name = "packageVersionLabel";
			this.packageVersionLabel.Xalign = 0F;
			this.packageVersionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Version:");
			this.generalTable.Add(this.packageVersionLabel);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.generalTable[this.packageVersionLabel]));
			w10.TopAttach = ((uint)(1));
			w10.BottomAttach = ((uint)(2));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child generalTable.Gtk.Table+TableChild
			this.packageVersionTextBox = new global::Gtk.Entry();
			this.packageVersionTextBox.CanFocus = true;
			this.packageVersionTextBox.Name = "packageVersionTextBox";
			this.packageVersionTextBox.IsEditable = true;
			this.packageVersionTextBox.InvisibleChar = '●';
			this.generalTable.Add(this.packageVersionTextBox);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.generalTable[this.packageVersionTextBox]));
			w11.TopAttach = ((uint)(1));
			w11.BottomAttach = ((uint)(2));
			w11.LeftAttach = ((uint)(1));
			w11.RightAttach = ((uint)(2));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			this.notebook.Add(this.generalTable);
			// Notebook tab
			this.generalTabPageLabel = new global::Gtk.Label();
			this.generalTabPageLabel.Name = "generalTabPageLabel";
			this.generalTabPageLabel.LabelProp = global::Mono.Unix.Catalog.GetString("General");
			this.notebook.SetTabLabel(this.generalTable, this.generalTabPageLabel);
			this.generalTabPageLabel.ShowAll();
			// Container child notebook.Gtk.Notebook+NotebookChild
			this.detailsTable = new global::Gtk.Table(((uint)(13)), ((uint)(3)), false);
			this.detailsTable.Name = "detailsTable";
			this.detailsTable.RowSpacing = ((uint)(6));
			this.detailsTable.ColumnSpacing = ((uint)(6));
			this.detailsTable.BorderWidth = ((uint)(10));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageCopyrightLabel = new global::Gtk.Label();
			this.packageCopyrightLabel.Name = "packageCopyrightLabel";
			this.packageCopyrightLabel.Xalign = 0F;
			this.packageCopyrightLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Copyright:");
			this.detailsTable.Add(this.packageCopyrightLabel);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageCopyrightLabel]));
			w13.TopAttach = ((uint)(1));
			w13.BottomAttach = ((uint)(2));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			w13.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageCopyrightTextBox = new global::Gtk.Entry();
			this.packageCopyrightTextBox.CanFocus = true;
			this.packageCopyrightTextBox.Name = "packageCopyrightTextBox";
			this.packageCopyrightTextBox.IsEditable = true;
			this.packageCopyrightTextBox.InvisibleChar = '●';
			this.detailsTable.Add(this.packageCopyrightTextBox);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageCopyrightTextBox]));
			w14.TopAttach = ((uint)(1));
			w14.BottomAttach = ((uint)(2));
			w14.LeftAttach = ((uint)(1));
			w14.RightAttach = ((uint)(2));
			w14.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageDevelopmentDependencyCheckBox = new global::Gtk.CheckButton();
			this.packageDevelopmentDependencyCheckBox.CanFocus = true;
			this.packageDevelopmentDependencyCheckBox.Name = "packageDevelopmentDependencyCheckBox";
			this.packageDevelopmentDependencyCheckBox.Label = "";
			this.packageDevelopmentDependencyCheckBox.DrawIndicator = true;
			this.packageDevelopmentDependencyCheckBox.UseUnderline = true;
			this.packageDevelopmentDependencyCheckBox.Xalign = 0F;
			this.detailsTable.Add(this.packageDevelopmentDependencyCheckBox);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageDevelopmentDependencyCheckBox]));
			w15.TopAttach = ((uint)(8));
			w15.BottomAttach = ((uint)(9));
			w15.LeftAttach = ((uint)(1));
			w15.RightAttach = ((uint)(2));
			w15.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageDevelopmentDependencyLabel = new global::Gtk.Label();
			this.packageDevelopmentDependencyLabel.Name = "packageDevelopmentDependencyLabel";
			this.packageDevelopmentDependencyLabel.Xalign = 0F;
			this.packageDevelopmentDependencyLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Development Dependency:");
			this.detailsTable.Add(this.packageDevelopmentDependencyLabel);
			global::Gtk.Table.TableChild w16 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageDevelopmentDependencyLabel]));
			w16.TopAttach = ((uint)(8));
			w16.BottomAttach = ((uint)(9));
			w16.XOptions = ((global::Gtk.AttachOptions)(4));
			w16.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageIconUrlLabel = new global::Gtk.Label();
			this.packageIconUrlLabel.Name = "packageIconUrlLabel";
			this.packageIconUrlLabel.Xalign = 0F;
			this.packageIconUrlLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Icon URL:");
			this.detailsTable.Add(this.packageIconUrlLabel);
			global::Gtk.Table.TableChild w17 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageIconUrlLabel]));
			w17.TopAttach = ((uint)(5));
			w17.BottomAttach = ((uint)(6));
			w17.XOptions = ((global::Gtk.AttachOptions)(4));
			w17.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageIconUrlTextBox = new global::Gtk.Entry();
			this.packageIconUrlTextBox.CanFocus = true;
			this.packageIconUrlTextBox.Name = "packageIconUrlTextBox";
			this.packageIconUrlTextBox.IsEditable = true;
			this.packageIconUrlTextBox.InvisibleChar = '●';
			this.detailsTable.Add(this.packageIconUrlTextBox);
			global::Gtk.Table.TableChild w18 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageIconUrlTextBox]));
			w18.TopAttach = ((uint)(5));
			w18.BottomAttach = ((uint)(6));
			w18.LeftAttach = ((uint)(1));
			w18.RightAttach = ((uint)(2));
			w18.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageLanguageHBox = new global::Gtk.HBox();
			this.packageLanguageHBox.Name = "packageLanguageHBox";
			this.packageLanguageHBox.Spacing = 6;
			// Container child packageLanguageHBox.Gtk.Box+BoxChild
			this.packageLanguageComboBox = global::Gtk.ComboBoxEntry.NewText();
			this.packageLanguageComboBox.Name = "packageLanguageComboBox";
			this.packageLanguageHBox.Add(this.packageLanguageComboBox);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.packageLanguageHBox[this.packageLanguageComboBox]));
			w19.Position = 0;
			w19.Expand = false;
			w19.Fill = false;
			this.detailsTable.Add(this.packageLanguageHBox);
			global::Gtk.Table.TableChild w20 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageLanguageHBox]));
			w20.TopAttach = ((uint)(10));
			w20.BottomAttach = ((uint)(11));
			w20.LeftAttach = ((uint)(1));
			w20.RightAttach = ((uint)(2));
			w20.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageLanguageLabel = new global::Gtk.Label();
			this.packageLanguageLabel.Name = "packageLanguageLabel";
			this.packageLanguageLabel.Xalign = 0F;
			this.packageLanguageLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Language:");
			this.detailsTable.Add(this.packageLanguageLabel);
			global::Gtk.Table.TableChild w21 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageLanguageLabel]));
			w21.TopAttach = ((uint)(10));
			w21.BottomAttach = ((uint)(11));
			w21.XOptions = ((global::Gtk.AttachOptions)(4));
			w21.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageLicenseUrlLabel = new global::Gtk.Label();
			this.packageLicenseUrlLabel.Name = "packageLicenseUrlLabel";
			this.packageLicenseUrlLabel.Xalign = 0F;
			this.packageLicenseUrlLabel.LabelProp = global::Mono.Unix.Catalog.GetString("License URL:");
			this.detailsTable.Add(this.packageLicenseUrlLabel);
			global::Gtk.Table.TableChild w22 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageLicenseUrlLabel]));
			w22.TopAttach = ((uint)(6));
			w22.BottomAttach = ((uint)(7));
			w22.XOptions = ((global::Gtk.AttachOptions)(4));
			w22.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageLicenseUrlTextBox = new global::Gtk.Entry();
			this.packageLicenseUrlTextBox.CanFocus = true;
			this.packageLicenseUrlTextBox.Name = "packageLicenseUrlTextBox";
			this.packageLicenseUrlTextBox.IsEditable = true;
			this.packageLicenseUrlTextBox.InvisibleChar = '●';
			this.detailsTable.Add(this.packageLicenseUrlTextBox);
			global::Gtk.Table.TableChild w23 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageLicenseUrlTextBox]));
			w23.TopAttach = ((uint)(6));
			w23.BottomAttach = ((uint)(7));
			w23.LeftAttach = ((uint)(1));
			w23.RightAttach = ((uint)(2));
			w23.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageOwnersLabel = new global::Gtk.Label();
			this.packageOwnersLabel.Name = "packageOwnersLabel";
			this.packageOwnersLabel.Xalign = 0F;
			this.packageOwnersLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Owners:");
			this.detailsTable.Add(this.packageOwnersLabel);
			global::Gtk.Table.TableChild w24 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageOwnersLabel]));
			w24.XOptions = ((global::Gtk.AttachOptions)(4));
			w24.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageOwnersTextBox = new global::Gtk.Entry();
			this.packageOwnersTextBox.CanFocus = true;
			this.packageOwnersTextBox.Name = "packageOwnersTextBox";
			this.packageOwnersTextBox.IsEditable = true;
			this.packageOwnersTextBox.InvisibleChar = '●';
			this.detailsTable.Add(this.packageOwnersTextBox);
			global::Gtk.Table.TableChild w25 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageOwnersTextBox]));
			w25.LeftAttach = ((uint)(1));
			w25.RightAttach = ((uint)(2));
			w25.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageProjectUrlLabel = new global::Gtk.Label();
			this.packageProjectUrlLabel.Name = "packageProjectUrlLabel";
			this.packageProjectUrlLabel.Xalign = 0F;
			this.packageProjectUrlLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Project URL:");
			this.detailsTable.Add(this.packageProjectUrlLabel);
			global::Gtk.Table.TableChild w26 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageProjectUrlLabel]));
			w26.TopAttach = ((uint)(4));
			w26.BottomAttach = ((uint)(5));
			w26.XOptions = ((global::Gtk.AttachOptions)(4));
			w26.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageProjectUrlTextBox = new global::Gtk.Entry();
			this.packageProjectUrlTextBox.CanFocus = true;
			this.packageProjectUrlTextBox.Name = "packageProjectUrlTextBox";
			this.packageProjectUrlTextBox.IsEditable = true;
			this.packageProjectUrlTextBox.InvisibleChar = '●';
			this.detailsTable.Add(this.packageProjectUrlTextBox);
			global::Gtk.Table.TableChild w27 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageProjectUrlTextBox]));
			w27.TopAttach = ((uint)(4));
			w27.BottomAttach = ((uint)(5));
			w27.LeftAttach = ((uint)(1));
			w27.RightAttach = ((uint)(2));
			w27.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageReleaseNotesLabelVBox = new global::Gtk.VBox();
			this.packageReleaseNotesLabelVBox.Name = "packageReleaseNotesLabelVBox";
			this.packageReleaseNotesLabelVBox.Spacing = 6;
			// Container child packageReleaseNotesLabelVBox.Gtk.Box+BoxChild
			this.packageReleaseNotesLabel = new global::Gtk.Label();
			this.packageReleaseNotesLabel.Name = "packageReleaseNotesLabel";
			this.packageReleaseNotesLabel.Xalign = 0F;
			this.packageReleaseNotesLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Release Notes:");
			this.packageReleaseNotesLabelVBox.Add(this.packageReleaseNotesLabel);
			global::Gtk.Box.BoxChild w28 = ((global::Gtk.Box.BoxChild)(this.packageReleaseNotesLabelVBox[this.packageReleaseNotesLabel]));
			w28.Position = 0;
			w28.Expand = false;
			w28.Fill = false;
			w28.Padding = ((uint)(6));
			// Container child packageReleaseNotesLabelVBox.Gtk.Box+BoxChild
			this.packageReleaseNotesPaddingLabel = new global::Gtk.Label();
			this.packageReleaseNotesPaddingLabel.Name = "packageReleaseNotesPaddingLabel";
			this.packageReleaseNotesPaddingLabel.Xalign = 0F;
			this.packageReleaseNotesLabelVBox.Add(this.packageReleaseNotesPaddingLabel);
			global::Gtk.Box.BoxChild w29 = ((global::Gtk.Box.BoxChild)(this.packageReleaseNotesLabelVBox[this.packageReleaseNotesPaddingLabel]));
			w29.Position = 1;
			w29.Expand = false;
			w29.Fill = false;
			this.detailsTable.Add(this.packageReleaseNotesLabelVBox);
			global::Gtk.Table.TableChild w30 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageReleaseNotesLabelVBox]));
			w30.TopAttach = ((uint)(11));
			w30.BottomAttach = ((uint)(12));
			w30.XOptions = ((global::Gtk.AttachOptions)(4));
			w30.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageReleaseNotesScrolledWindow = new global::Gtk.ScrolledWindow();
			this.packageReleaseNotesScrolledWindow.Name = "packageReleaseNotesScrolledWindow";
			this.packageReleaseNotesScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child packageReleaseNotesScrolledWindow.Gtk.Container+ContainerChild
			this.packageReleaseNotesTextView = new global::Gtk.TextView();
			this.packageReleaseNotesTextView.CanFocus = true;
			this.packageReleaseNotesTextView.Name = "packageReleaseNotesTextView";
			this.packageReleaseNotesTextView.AcceptsTab = false;
			this.packageReleaseNotesTextView.WrapMode = ((global::Gtk.WrapMode)(2));
			this.packageReleaseNotesScrolledWindow.Add(this.packageReleaseNotesTextView);
			this.detailsTable.Add(this.packageReleaseNotesScrolledWindow);
			global::Gtk.Table.TableChild w32 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageReleaseNotesScrolledWindow]));
			w32.TopAttach = ((uint)(11));
			w32.BottomAttach = ((uint)(12));
			w32.LeftAttach = ((uint)(1));
			w32.RightAttach = ((uint)(2));
			w32.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageRequireLicenseAcceptanceCheckBox = new global::Gtk.CheckButton();
			this.packageRequireLicenseAcceptanceCheckBox.CanFocus = true;
			this.packageRequireLicenseAcceptanceCheckBox.Name = "packageRequireLicenseAcceptanceCheckBox";
			this.packageRequireLicenseAcceptanceCheckBox.Label = "";
			this.packageRequireLicenseAcceptanceCheckBox.DrawIndicator = true;
			this.packageRequireLicenseAcceptanceCheckBox.UseUnderline = true;
			this.packageRequireLicenseAcceptanceCheckBox.Xalign = 0F;
			this.detailsTable.Add(this.packageRequireLicenseAcceptanceCheckBox);
			global::Gtk.Table.TableChild w33 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageRequireLicenseAcceptanceCheckBox]));
			w33.TopAttach = ((uint)(7));
			w33.BottomAttach = ((uint)(8));
			w33.LeftAttach = ((uint)(1));
			w33.RightAttach = ((uint)(2));
			w33.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageRequireLicenseAcceptanceLabel = new global::Gtk.Label();
			this.packageRequireLicenseAcceptanceLabel.Name = "packageRequireLicenseAcceptanceLabel";
			this.packageRequireLicenseAcceptanceLabel.Xalign = 0F;
			this.packageRequireLicenseAcceptanceLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Require License Acceptance:");
			this.detailsTable.Add(this.packageRequireLicenseAcceptanceLabel);
			global::Gtk.Table.TableChild w34 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageRequireLicenseAcceptanceLabel]));
			w34.TopAttach = ((uint)(7));
			w34.BottomAttach = ((uint)(8));
			w34.XOptions = ((global::Gtk.AttachOptions)(4));
			w34.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageSummaryLabel = new global::Gtk.Label();
			this.packageSummaryLabel.Name = "packageSummaryLabel";
			this.packageSummaryLabel.Xalign = 0F;
			this.packageSummaryLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Summary:");
			this.detailsTable.Add(this.packageSummaryLabel);
			global::Gtk.Table.TableChild w35 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageSummaryLabel]));
			w35.TopAttach = ((uint)(3));
			w35.BottomAttach = ((uint)(4));
			w35.XOptions = ((global::Gtk.AttachOptions)(4));
			w35.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageSummaryTextBox = new global::Gtk.Entry();
			this.packageSummaryTextBox.CanFocus = true;
			this.packageSummaryTextBox.Name = "packageSummaryTextBox";
			this.packageSummaryTextBox.IsEditable = true;
			this.packageSummaryTextBox.InvisibleChar = '●';
			this.detailsTable.Add(this.packageSummaryTextBox);
			global::Gtk.Table.TableChild w36 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageSummaryTextBox]));
			w36.TopAttach = ((uint)(3));
			w36.BottomAttach = ((uint)(4));
			w36.LeftAttach = ((uint)(1));
			w36.RightAttach = ((uint)(2));
			w36.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageTagsLabel = new global::Gtk.Label();
			this.packageTagsLabel.Name = "packageTagsLabel";
			this.packageTagsLabel.Xalign = 0F;
			this.packageTagsLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Tags:");
			this.detailsTable.Add(this.packageTagsLabel);
			global::Gtk.Table.TableChild w37 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageTagsLabel]));
			w37.TopAttach = ((uint)(9));
			w37.BottomAttach = ((uint)(10));
			w37.XOptions = ((global::Gtk.AttachOptions)(4));
			w37.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageTagsTextBox = new global::Gtk.Entry();
			this.packageTagsTextBox.CanFocus = true;
			this.packageTagsTextBox.Name = "packageTagsTextBox";
			this.packageTagsTextBox.IsEditable = true;
			this.packageTagsTextBox.InvisibleChar = '●';
			this.detailsTable.Add(this.packageTagsTextBox);
			global::Gtk.Table.TableChild w38 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageTagsTextBox]));
			w38.TopAttach = ((uint)(9));
			w38.BottomAttach = ((uint)(10));
			w38.LeftAttach = ((uint)(1));
			w38.RightAttach = ((uint)(2));
			w38.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageTitleLabel = new global::Gtk.Label();
			this.packageTitleLabel.Name = "packageTitleLabel";
			this.packageTitleLabel.Xalign = 0F;
			this.packageTitleLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Title:");
			this.detailsTable.Add(this.packageTitleLabel);
			global::Gtk.Table.TableChild w39 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageTitleLabel]));
			w39.TopAttach = ((uint)(2));
			w39.BottomAttach = ((uint)(3));
			w39.XOptions = ((global::Gtk.AttachOptions)(4));
			w39.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child detailsTable.Gtk.Table+TableChild
			this.packageTitleTextBox = new global::Gtk.Entry();
			this.packageTitleTextBox.CanFocus = true;
			this.packageTitleTextBox.Name = "packageTitleTextBox";
			this.packageTitleTextBox.IsEditable = true;
			this.packageTitleTextBox.InvisibleChar = '●';
			this.detailsTable.Add(this.packageTitleTextBox);
			global::Gtk.Table.TableChild w40 = ((global::Gtk.Table.TableChild)(this.detailsTable[this.packageTitleTextBox]));
			w40.TopAttach = ((uint)(2));
			w40.BottomAttach = ((uint)(3));
			w40.LeftAttach = ((uint)(1));
			w40.RightAttach = ((uint)(2));
			w40.YOptions = ((global::Gtk.AttachOptions)(4));
			this.notebook.Add(this.detailsTable);
			global::Gtk.Notebook.NotebookChild w41 = ((global::Gtk.Notebook.NotebookChild)(this.notebook[this.detailsTable]));
			w41.Position = 1;
			// Notebook tab
			this.detailsTabPageLabel = new global::Gtk.Label();
			this.detailsTabPageLabel.Name = "detailsTabPageLabel";
			this.detailsTabPageLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Details");
			this.notebook.SetTabLabel(this.detailsTable, this.detailsTabPageLabel);
			this.detailsTabPageLabel.ShowAll();
			this.Add(this.notebook);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.generalTabPageLabel.Hide();
			this.Hide();
		}
	}
}
#pragma warning restore 436
