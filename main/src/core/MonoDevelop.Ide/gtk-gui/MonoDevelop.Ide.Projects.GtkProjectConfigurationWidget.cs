#pragma warning disable 436

namespace MonoDevelop.Ide.Projects
{
	internal partial class GtkProjectConfigurationWidget
	{
		private global::Gtk.HBox mainHBox;

		private global::Gtk.EventBox leftBorderEventBox;

		private global::Gtk.EventBox projectConfigurationTableEventBox;

		private global::Gtk.VBox projectConfigurationVBox;

		private global::Gtk.EventBox projectConfigurationTopEventBox;

		private global::Gtk.Table projectConfigurationTable;

		private global::Gtk.Button browseButton;

		private global::Gtk.CheckButton createGitIgnoreFileCheckBox;

		private global::Gtk.CheckButton createProjectWithinSolutionDirectoryCheckBox;

		private global::Gtk.Label locationLabel;

		private global::Gtk.DrawingArea locationSeparator;

		private global::Gtk.Entry locationTextBox;

		private global::Gtk.Label projectNameLabel;

		private global::Gtk.Entry projectNameTextBox;

		private global::Gtk.Label solutionNameLabel;

		private global::Gtk.DrawingArea solutionNameSeparator;

		private global::Gtk.Entry solutionNameTextBox;

		private global::Gtk.CheckButton useGitCheckBox;

		private global::Gtk.HBox versionControlLabelHBox;

		private global::Gtk.Label versionControlSpacerLabel;

		private global::Gtk.Label versionControlLabel;

		private global::Gtk.EventBox projectConfigurationBottomEventBox;

		private global::Gtk.EventBox projectConfigurationRightBorderEventBox;

		private global::Gtk.EventBox eventBox;

		private global::Gtk.VBox previewProjectFolderVBox;

		private global::MonoDevelop.Ide.Projects.GtkProjectFolderPreviewWidget projectFolderPreviewWidget;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.GtkProjectConfigurationWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.GtkProjectConfigurationWidget";
			// Container child MonoDevelop.Ide.Projects.GtkProjectConfigurationWidget.Gtk.Container+ContainerChild
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
			this.projectConfigurationTableEventBox = new global::Gtk.EventBox();
			this.projectConfigurationTableEventBox.WidthRequest = 561;
			this.projectConfigurationTableEventBox.Name = "projectConfigurationTableEventBox";
			// Container child projectConfigurationTableEventBox.Gtk.Container+ContainerChild
			this.projectConfigurationVBox = new global::Gtk.VBox();
			this.projectConfigurationVBox.Name = "projectConfigurationVBox";
			// Container child projectConfigurationVBox.Gtk.Box+BoxChild
			this.projectConfigurationTopEventBox = new global::Gtk.EventBox();
			this.projectConfigurationTopEventBox.Name = "projectConfigurationTopEventBox";
			this.projectConfigurationVBox.Add(this.projectConfigurationTopEventBox);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.projectConfigurationVBox[this.projectConfigurationTopEventBox]));
			w2.Position = 0;
			// Container child projectConfigurationVBox.Gtk.Box+BoxChild
			this.projectConfigurationTable = new global::Gtk.Table(((uint)(8)), ((uint)(3)), false);
			this.projectConfigurationTable.Name = "projectConfigurationTable";
			this.projectConfigurationTable.RowSpacing = ((uint)(7));
			this.projectConfigurationTable.ColumnSpacing = ((uint)(6));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.browseButton = new global::Gtk.Button();
			this.browseButton.CanFocus = true;
			this.browseButton.Name = "browseButton";
			this.browseButton.UseUnderline = true;
			this.browseButton.BorderWidth = ((uint)(1));
			this.browseButton.Label = global::Mono.Unix.Catalog.GetString("Browse...");
			this.projectConfigurationTable.Add(this.browseButton);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.browseButton]));
			w3.TopAttach = ((uint)(3));
			w3.BottomAttach = ((uint)(4));
			w3.LeftAttach = ((uint)(2));
			w3.RightAttach = ((uint)(3));
			w3.XPadding = ((uint)(5));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.createGitIgnoreFileCheckBox = new global::Gtk.CheckButton();
			this.createGitIgnoreFileCheckBox.CanFocus = true;
			this.createGitIgnoreFileCheckBox.Name = "createGitIgnoreFileCheckBox";
			this.createGitIgnoreFileCheckBox.Label = global::Mono.Unix.Catalog.GetString("Create a .gitignore file to ignore inessential files.");
			this.createGitIgnoreFileCheckBox.Active = true;
			this.createGitIgnoreFileCheckBox.DrawIndicator = true;
			this.createGitIgnoreFileCheckBox.UseUnderline = true;
			this.projectConfigurationTable.Add(this.createGitIgnoreFileCheckBox);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.createGitIgnoreFileCheckBox]));
			w4.TopAttach = ((uint)(7));
			w4.BottomAttach = ((uint)(8));
			w4.LeftAttach = ((uint)(1));
			w4.RightAttach = ((uint)(3));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.createProjectWithinSolutionDirectoryCheckBox = new global::Gtk.CheckButton();
			this.createProjectWithinSolutionDirectoryCheckBox.CanFocus = true;
			this.createProjectWithinSolutionDirectoryCheckBox.Name = "createProjectWithinSolutionDirectoryCheckBox";
			this.createProjectWithinSolutionDirectoryCheckBox.Label = global::Mono.Unix.Catalog.GetString("Create a project directory within the solution directory.");
			this.createProjectWithinSolutionDirectoryCheckBox.Active = true;
			this.createProjectWithinSolutionDirectoryCheckBox.DrawIndicator = true;
			this.createProjectWithinSolutionDirectoryCheckBox.UseUnderline = true;
			this.projectConfigurationTable.Add(this.createProjectWithinSolutionDirectoryCheckBox);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.createProjectWithinSolutionDirectoryCheckBox]));
			w5.TopAttach = ((uint)(4));
			w5.BottomAttach = ((uint)(5));
			w5.LeftAttach = ((uint)(1));
			w5.RightAttach = ((uint)(3));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.locationLabel = new global::Gtk.Label();
			this.locationLabel.Name = "locationLabel";
			this.locationLabel.Xpad = 5;
			this.locationLabel.Xalign = 1F;
			this.locationLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Location:");
			this.locationLabel.Justify = ((global::Gtk.Justification)(1));
			this.projectConfigurationTable.Add(this.locationLabel);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.locationLabel]));
			w6.TopAttach = ((uint)(3));
			w6.BottomAttach = ((uint)(4));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.locationSeparator = new global::Gtk.DrawingArea();
			this.locationSeparator.HeightRequest = 1;
			this.locationSeparator.Name = "locationSeparator";
			this.projectConfigurationTable.Add(this.locationSeparator);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.locationSeparator]));
			w7.TopAttach = ((uint)(5));
			w7.BottomAttach = ((uint)(6));
			w7.RightAttach = ((uint)(3));
			w7.YPadding = ((uint)(10));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.locationTextBox = new global::Gtk.Entry();
			this.locationTextBox.CanFocus = true;
			this.locationTextBox.Name = "locationTextBox";
			this.locationTextBox.IsEditable = true;
			this.locationTextBox.InvisibleChar = '●';
			this.projectConfigurationTable.Add(this.locationTextBox);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.locationTextBox]));
			w8.TopAttach = ((uint)(3));
			w8.BottomAttach = ((uint)(4));
			w8.LeftAttach = ((uint)(1));
			w8.RightAttach = ((uint)(2));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.projectNameLabel = new global::Gtk.Label();
			this.projectNameLabel.Name = "projectNameLabel";
			this.projectNameLabel.Xpad = 5;
			this.projectNameLabel.Xalign = 1F;
			this.projectNameLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Project Name:");
			this.projectNameLabel.Justify = ((global::Gtk.Justification)(1));
			this.projectConfigurationTable.Add(this.projectNameLabel);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.projectNameLabel]));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.projectNameTextBox = new global::Gtk.Entry();
			this.projectNameTextBox.CanFocus = true;
			this.projectNameTextBox.Name = "projectNameTextBox";
			this.projectNameTextBox.IsEditable = true;
			this.projectNameTextBox.InvisibleChar = '●';
			this.projectConfigurationTable.Add(this.projectNameTextBox);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.projectNameTextBox]));
			w10.LeftAttach = ((uint)(1));
			w10.RightAttach = ((uint)(2));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.solutionNameLabel = new global::Gtk.Label();
			this.solutionNameLabel.Name = "solutionNameLabel";
			this.solutionNameLabel.Xpad = 5;
			this.solutionNameLabel.Xalign = 1F;
			this.solutionNameLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Solution Name:");
			this.solutionNameLabel.Justify = ((global::Gtk.Justification)(1));
			this.projectConfigurationTable.Add(this.solutionNameLabel);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.solutionNameLabel]));
			w11.TopAttach = ((uint)(1));
			w11.BottomAttach = ((uint)(2));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.solutionNameSeparator = new global::Gtk.DrawingArea();
			this.solutionNameSeparator.HeightRequest = 1;
			this.solutionNameSeparator.Name = "solutionNameSeparator";
			this.projectConfigurationTable.Add(this.solutionNameSeparator);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.solutionNameSeparator]));
			w12.TopAttach = ((uint)(2));
			w12.BottomAttach = ((uint)(3));
			w12.RightAttach = ((uint)(3));
			w12.YPadding = ((uint)(10));
			w12.XOptions = ((global::Gtk.AttachOptions)(4));
			w12.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.solutionNameTextBox = new global::Gtk.Entry();
			this.solutionNameTextBox.CanFocus = true;
			this.solutionNameTextBox.Name = "solutionNameTextBox";
			this.solutionNameTextBox.IsEditable = true;
			this.solutionNameTextBox.InvisibleChar = '●';
			this.projectConfigurationTable.Add(this.solutionNameTextBox);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.solutionNameTextBox]));
			w13.TopAttach = ((uint)(1));
			w13.BottomAttach = ((uint)(2));
			w13.LeftAttach = ((uint)(1));
			w13.RightAttach = ((uint)(2));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			w13.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.useGitCheckBox = new global::Gtk.CheckButton();
			this.useGitCheckBox.CanFocus = true;
			this.useGitCheckBox.Name = "useGitCheckBox";
			this.useGitCheckBox.Label = global::Mono.Unix.Catalog.GetString("Use git for version control.");
			this.useGitCheckBox.Active = true;
			this.useGitCheckBox.DrawIndicator = true;
			this.useGitCheckBox.UseUnderline = true;
			this.projectConfigurationTable.Add(this.useGitCheckBox);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.useGitCheckBox]));
			w14.TopAttach = ((uint)(6));
			w14.BottomAttach = ((uint)(7));
			w14.LeftAttach = ((uint)(1));
			w14.RightAttach = ((uint)(2));
			w14.XOptions = ((global::Gtk.AttachOptions)(4));
			w14.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child projectConfigurationTable.Gtk.Table+TableChild
			this.versionControlLabelHBox = new global::Gtk.HBox();
			this.versionControlLabelHBox.Name = "versionControlLabelHBox";
			// Container child versionControlLabelHBox.Gtk.Box+BoxChild
			this.versionControlSpacerLabel = new global::Gtk.Label();
			this.versionControlSpacerLabel.WidthRequest = 24;
			this.versionControlSpacerLabel.Name = "versionControlSpacerLabel";
			this.versionControlSpacerLabel.Justify = ((global::Gtk.Justification)(1));
			this.versionControlLabelHBox.Add(this.versionControlSpacerLabel);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.versionControlLabelHBox[this.versionControlSpacerLabel]));
			w15.Position = 0;
			w15.Expand = false;
			w15.Fill = false;
			// Container child versionControlLabelHBox.Gtk.Box+BoxChild
			this.versionControlLabel = new global::Gtk.Label();
			this.versionControlLabel.Name = "versionControlLabel";
			this.versionControlLabel.Xpad = 5;
			this.versionControlLabel.Xalign = 1F;
			this.versionControlLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Version Control:");
			this.versionControlLabel.Justify = ((global::Gtk.Justification)(1));
			this.versionControlLabelHBox.Add(this.versionControlLabel);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.versionControlLabelHBox[this.versionControlLabel]));
			w16.Position = 1;
			w16.Expand = false;
			w16.Fill = false;
			this.projectConfigurationTable.Add(this.versionControlLabelHBox);
			global::Gtk.Table.TableChild w17 = ((global::Gtk.Table.TableChild)(this.projectConfigurationTable[this.versionControlLabelHBox]));
			w17.TopAttach = ((uint)(6));
			w17.BottomAttach = ((uint)(7));
			w17.XOptions = ((global::Gtk.AttachOptions)(4));
			w17.YOptions = ((global::Gtk.AttachOptions)(4));
			this.projectConfigurationVBox.Add(this.projectConfigurationTable);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.projectConfigurationVBox[this.projectConfigurationTable]));
			w18.Position = 1;
			w18.Expand = false;
			w18.Fill = false;
			// Container child projectConfigurationVBox.Gtk.Box+BoxChild
			this.projectConfigurationBottomEventBox = new global::Gtk.EventBox();
			this.projectConfigurationBottomEventBox.Name = "projectConfigurationBottomEventBox";
			this.projectConfigurationVBox.Add(this.projectConfigurationBottomEventBox);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.projectConfigurationVBox[this.projectConfigurationBottomEventBox]));
			w19.Position = 2;
			this.projectConfigurationTableEventBox.Add(this.projectConfigurationVBox);
			this.mainHBox.Add(this.projectConfigurationTableEventBox);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.projectConfigurationTableEventBox]));
			w21.Position = 1;
			// Container child mainHBox.Gtk.Box+BoxChild
			this.projectConfigurationRightBorderEventBox = new global::Gtk.EventBox();
			this.projectConfigurationRightBorderEventBox.WidthRequest = 30;
			this.projectConfigurationRightBorderEventBox.Name = "projectConfigurationRightBorderEventBox";
			this.mainHBox.Add(this.projectConfigurationRightBorderEventBox);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.projectConfigurationRightBorderEventBox]));
			w22.Position = 2;
			w22.Expand = false;
			// Container child mainHBox.Gtk.Box+BoxChild
			this.eventBox = new global::Gtk.EventBox();
			this.eventBox.Name = "eventBox";
			// Container child eventBox.Gtk.Container+ContainerChild
			this.previewProjectFolderVBox = new global::Gtk.VBox();
			this.previewProjectFolderVBox.Name = "previewProjectFolderVBox";
			this.previewProjectFolderVBox.Spacing = 6;
			this.previewProjectFolderVBox.BorderWidth = ((uint)(20));
			// Container child previewProjectFolderVBox.Gtk.Box+BoxChild
			this.projectFolderPreviewWidget = null;
			this.previewProjectFolderVBox.Add(this.projectFolderPreviewWidget);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.previewProjectFolderVBox[this.projectFolderPreviewWidget]));
			w23.Position = 0;
			this.eventBox.Add(this.previewProjectFolderVBox);
			this.mainHBox.Add(this.eventBox);
			global::Gtk.Box.BoxChild w25 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.eventBox]));
			w25.Position = 3;
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
