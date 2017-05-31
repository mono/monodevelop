//
// GtkProjectConfigurationWidget.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Projects
{
	[System.ComponentModel.ToolboxItem (true)]
	partial class GtkProjectConfigurationWidget : Gtk.Bin
	{
		FinalProjectConfigurationPage projectConfiguration;
		uint defaultTableRows;
		Gdk.Color separatorColor = Styles.NewProjectDialog.ProjectConfigurationSeparatorColor.ToGdkColor ();
		Gdk.Color leftHandBackgroundColor = Styles.NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor.ToGdkColor ();
		DrawingArea extraControlsSeparator;
		List<ExtraControlTableRow> extraControlRows = new List<ExtraControlTableRow> ();

		public GtkProjectConfigurationWidget ()
		{
			this.Build ();

			solutionNameSeparator.ModifyBg (StateType.Normal, separatorColor);
			locationSeparator.ModifyBg (StateType.Normal, separatorColor);

			eventBox.ModifyBg (StateType.Normal, Styles.NewProjectDialog.ProjectConfigurationRightHandBackgroundColor.ToGdkColor ());

			leftBorderEventBox.ModifyBg (StateType.Normal, leftHandBackgroundColor);
			projectConfigurationRightBorderEventBox.ModifyBg (StateType.Normal, leftHandBackgroundColor);
			projectConfigurationTopEventBox.ModifyBg (StateType.Normal, leftHandBackgroundColor);
			projectConfigurationTableEventBox.ModifyBg (StateType.Normal, leftHandBackgroundColor);
			projectConfigurationBottomEventBox.ModifyBg (StateType.Normal, leftHandBackgroundColor);

			projectNameTextBox.ActivatesDefault = true;
			solutionNameTextBox.ActivatesDefault = true;
			locationTextBox.ActivatesDefault = true;

			projectNameTextBox.TruncateMultiline = true;
			solutionNameTextBox.TruncateMultiline = true;
			locationTextBox.TruncateMultiline = true;

			defaultTableRows = projectConfigurationTable.NRows;

			// Disable width request for the event box otherwise the New Project dialog
			// does not resize to fit. Set a width request for the project name text box
			// instead so the project folder preview widget does not take up more width.
			projectConfigurationTableEventBox.WidthRequest = -1;
			projectNameTextBox.WidthRequest = 335;

			RegisterEvents ();

			// Accessibility
			projectNameLabel.Accessible.Name = "ProjectConfigurationWidget.ProjectNameLabel";
			projectNameLabel.Accessible.SetTitleFor (projectNameTextBox.Accessible);

			projectNameTextBox.Accessible.Name = "ProjectConfigurationWidget.ProjectNameTextBox";
			projectNameTextBox.Accessible.Description = GettextCatalog.GetString ("Enter the name for the new project");
			projectNameTextBox.Accessible.SetTitleUIElement (projectNameLabel.Accessible);

			locationLabel.Accessible.Name = "ProjectConfigurationWidget.LocationLabel";
			locationLabel.Accessible.SetTitleFor (locationTextBox.Accessible, browseButton.Accessible);

			locationTextBox.Accessible.Name = "ProjectConfigurationWidget.LocationTextBox";
			locationTextBox.Accessible.Description = GettextCatalog.GetString ("Enter the filepath for the new project");
			locationTextBox.Accessible.SetTitleUIElement (locationLabel.Accessible);

			browseButton.Accessible.Name = "ProjectConfigurationWidget.BrowseButton";
			browseButton.Accessible.Description = GettextCatalog.GetString ("Open a file selector to select the project path");
			browseButton.Accessible.SetTitleUIElement (locationLabel.Accessible);

			createProjectWithinSolutionDirectoryCheckBox.Accessible.Name = "ProjectConfigurationWidget.CreateProjectDirectoryCheckBox";
			createProjectWithinSolutionDirectoryCheckBox.Accessible.Description = GettextCatalog.GetString ("Select whether a folder for the new project should be created inside the solution folder");

			versionControlLabel.Accessible.Name = "ProjectConfigurationWidget.VersionControl";
			versionControlLabel.Accessible.AddLinkedUIElement (useGitCheckBox.Accessible);
			versionControlLabel.Accessible.AddLinkedUIElement (createGitIgnoreFileCheckBox.Accessible);

			useGitCheckBox.Accessible.Name = "ProjectConfigurationWidget.UseGitCheckBox";
			useGitCheckBox.Accessible.Description = GettextCatalog.GetString ("Select whether to use Git source control");

			createGitIgnoreFileCheckBox.Accessible.Name = "ProjectConfigurationWidget.CreateGitIgnoreCheckBox";
			createGitIgnoreFileCheckBox.Accessible.Description = GettextCatalog.GetString ("Select whether to create a .gitignore file to ignore common files");

			projectFolderPreviewWidget.Accessible.Name = "ProjectConfigurationWidget.ProjectFolderPreview";
			projectFolderPreviewWidget.Accessible.SetLabel (GettextCatalog.GetString ("Folder Layout Preview"));
			projectFolderPreviewWidget.Accessible.Description = GettextCatalog.GetString ("A preview of the folder layout for the new project");
		}

		protected override void OnFocusGrabbed ()
		{
			if (projectConfiguration != null) {
				if (projectConfiguration.IsProjectNameEnabled) {
					projectNameTextBox.GrabFocus ();
				} else if (projectConfiguration.IsSolutionNameEnabled) {
					solutionNameTextBox.GrabFocus ();
				}
			} else {
				projectNameTextBox.GrabFocus ();
			}
		}

		void RegisterEvents ()
		{
			locationTextBox.Changed += (sender, e) => OnLocationTextBoxChanged ();
			projectNameTextBox.TextInserted += ProjectNameTextInserted;
			projectNameTextBox.Changed += (sender, e) => OnProjectNameTextBoxChanged ();
			solutionNameTextBox.Changed += (sender, e) => OnSolutionNameTextBoxChanged ();
			createGitIgnoreFileCheckBox.Clicked += (sender, e) => OnCreateGitIgnoreFileCheckBoxClicked ();
			useGitCheckBox.Clicked += (sender, e) => OnUseGitCheckBoxClicked ();
			createProjectWithinSolutionDirectoryCheckBox.Clicked += (sender, e) => OnCreateProjectWithinSolutionDirectoryCheckBoxClicked ();
			browseButton.Clicked += (sender, e) => BrowseButtonClicked ();
		}

		void OnLocationTextBoxChanged ()
		{
			projectConfiguration.Location = locationTextBox.Text;
			projectFolderPreviewWidget.UpdateLocation ();
		}

		void ProjectNameTextInserted (object o, TextInsertedArgs args)
		{
			if (args.Text.IndexOf ('\r') >= 0) {
				var textBox = (Entry)o;
				textBox.Text = textBox.Text.Replace ("\r", string.Empty);
			}
		}

		void OnProjectNameTextBoxChanged ()
		{
			projectConfiguration.ProjectName = projectNameTextBox.Text;
			solutionNameTextBox.Text = projectConfiguration.SolutionName;
			projectFolderPreviewWidget.UpdateProjectName ();
			projectFolderPreviewWidget.UpdateSolutionName ();
		}

		void OnSolutionNameTextBoxChanged ()
		{
			projectConfiguration.SolutionName = solutionNameTextBox.Text;
			projectFolderPreviewWidget.UpdateSolutionName ();
		}

		void OnCreateGitIgnoreFileCheckBoxClicked ()
		{
			projectConfiguration.CreateGitIgnoreFile = createGitIgnoreFileCheckBox.Active;
			projectFolderPreviewWidget.ShowGitIgnoreFile ();
		}

		void OnUseGitCheckBoxClicked ()
		{
			projectConfiguration.UseGit = useGitCheckBox.Active;
			createGitIgnoreFileCheckBox.Sensitive = projectConfiguration.IsGitIgnoreEnabled;
			projectFolderPreviewWidget.ShowGitFolder ();
			projectFolderPreviewWidget.ShowGitIgnoreFile ();
		}

		void OnCreateProjectWithinSolutionDirectoryCheckBoxClicked ()
		{
			projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory = createProjectWithinSolutionDirectoryCheckBox.Active;
			projectFolderPreviewWidget.Refresh ();
		}

		void BrowseButtonClicked ()
		{
			FilePath startingFolder = GetStartingFolder ();
			FilePath selectedFolder = BrowseForFolder (startingFolder);
			if (selectedFolder != null) {
				locationTextBox.Text = selectedFolder;
			}
		}

		FilePath GetStartingFolder ()
		{
			try {
				FilePath folder = locationTextBox.Text;

				if (!folder.IsNullOrEmpty && !folder.IsDirectory) {
					folder = folder.ParentDirectory;
					if (!folder.IsNullOrEmpty && !folder.IsDirectory)
						folder = FilePath.Null;
				}
				return folder;

			} catch (FileNotFoundException) {
			}

			return FilePath.Null;
		}

		FilePath BrowseForFolder (FilePath startingFolder)
		{
			var dialog = new SelectFolderDialog ();
			if (startingFolder != null)
				dialog.CurrentFolder = startingFolder;

			dialog.TransientFor = Toplevel as Gtk.Window;

			if (dialog.Run ())
				return dialog.SelectedFile;
			return null;
		}

		public void Load (FinalProjectConfigurationPage projectConfiguration, IEnumerable<ProjectConfigurationControl> controls)
		{
			this.projectConfiguration = projectConfiguration;
			LoadWidget ();
			AddExtraControls (controls.ToList ());
		}

		void LoadWidget ()
		{
			projectFolderPreviewWidget.Load (projectConfiguration);
			solutionNameLabel.Text = GetSolutionNameLabel ();
			locationTextBox.Text = projectConfiguration.Location;
			projectNameTextBox.Text = projectConfiguration.ProjectName;
			solutionNameTextBox.Text = projectConfiguration.SolutionName;

			solutionNameTextBox.Sensitive = projectConfiguration.IsSolutionNameEnabled;
			projectNameTextBox.Sensitive = projectConfiguration.IsProjectNameEnabled;
			createProjectWithinSolutionDirectoryCheckBox.Sensitive = projectConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled;
			createProjectWithinSolutionDirectoryCheckBox.Active = projectConfiguration.IsCreateProjectDirectoryInsideSolutionDirectoryEnabled ? projectConfiguration.CreateProjectDirectoryInsideSolutionDirectory : true;
			useGitCheckBox.Sensitive = projectConfiguration.IsUseGitEnabled;
			useGitCheckBox.Active = projectConfiguration.UseGit;
			createGitIgnoreFileCheckBox.Sensitive = projectConfiguration.IsGitIgnoreEnabled;
			createGitIgnoreFileCheckBox.Active = projectConfiguration.CreateGitIgnoreFile;
		}

		string GetSolutionNameLabel ()
		{
			if (projectConfiguration.IsWorkspace) {
				return GettextCatalog.GetString ("Workspace Name:");
			}
			return GettextCatalog.GetString ("Solution Name:");
		}

		void AddExtraControls (List<ProjectConfigurationControl> controls)
		{
			RemoveExistingExtraControls ();

			if (controls.Any ())
				AddExtraControlsSeparator ();

			foreach (ProjectConfigurationControl control in controls) {
				AddExtraControl (control);
			}
		}

		void RemoveExistingExtraControls ()
		{
			if (!extraControlRows.Any ())
				return;

			for (int i = extraControlRows.Count - 1; i >= 0; i--) {
				RemoveExtraControl (extraControlRows [i]);
			}

			RemoveExtraControlsSeparator ();
		}

		void RemoveExtraControl (ExtraControlTableRow extraRow)
		{
			if (extraRow.Label != null) {
				projectConfigurationTable.Remove (extraRow.Label);
				extraRow.Label.Dispose ();
			}

			if (extraRow.MainWidget.Parent != null)
				projectConfigurationTable.Remove (extraRow.MainWidget);

			if (extraRow.InformationTooltipWidget != null) {
				projectConfigurationTable.Remove (extraRow.InformationTooltipWidget);
				extraRow.InformationTooltipWidget.Dispose ();
				extraRow.InformationTooltip.Dispose ();
			}

			extraControlRows.Remove (extraRow);
			projectConfigurationTable.NRows--;
		}

		void RemoveExtraControlsSeparator ()
		{
			if (extraControlsSeparator != null) {
				projectConfigurationTable.Remove (extraControlsSeparator);

				extraControlsSeparator.Dispose ();
				extraControlsSeparator = null;

				projectConfigurationTable.NRows--;
			}
		}

		void AddExtraControlsSeparator ()
		{
			projectConfigurationTable.NRows++;

			extraControlsSeparator = new DrawingArea ();
			extraControlsSeparator.HeightRequest = 1;
			extraControlsSeparator.ModifyBg (StateType.Normal, separatorColor);
			projectConfigurationTable.Attach (
				extraControlsSeparator,
				0,
				3,
				defaultTableRows,
				defaultTableRows + 1,
				AttachOptions.Fill,
				(AttachOptions)0,
				0,
				10);
		}

		void AddExtraControl (ProjectConfigurationControl control)
		{
			var extraRow = new ExtraControlTableRow {
				Row = projectConfigurationTable.NRows,
				Label = new Label (control.Label ?? string.Empty) {
					Xpad = 5,
					Xalign = 1,
					Justify = Justification.Left
				},
				MainWidget = (Widget)control
			};

			CreateTooltip (control, extraRow);

			AddExtraControl (extraRow);
		}

		void CreateTooltip (ProjectConfigurationControl control, ExtraControlTableRow extraRow)
		{
			if (string.IsNullOrEmpty (control.InformationTooltip))
				return;

			var hbox = new HBox ();
			var paddingEventBox = new EventBox ();
			paddingEventBox.ModifyBg (StateType.Normal, leftHandBackgroundColor);
			hbox.PackStart (paddingEventBox, true, true, 0);

			var tooltipEventBox = new EventBox {
				HeightRequest = 16,
				WidthRequest = 16,
				VisibleWindow = false
			};

			hbox.PackStart (tooltipEventBox, false, false, 0);

			extraRow.InformationTooltipWidget = hbox;
			extraRow.InformationTooltip = CreateTooltip (tooltipEventBox, control.InformationTooltip);
		}

		EventBoxTooltip CreateTooltip (EventBox eventBox, string tooltipText)
		{
			Xwt.Drawing.Image image = ImageService.GetIcon ("md-help");
			eventBox.ModifyBg (StateType.Normal, leftHandBackgroundColor);
			eventBox.Add (new ImageView (image));
			eventBox.ShowAll ();

			return new EventBoxTooltip (eventBox) {
				ToolTip = GettextCatalog.GetString (tooltipText),
				Severity = TaskSeverity.Information
			};
		}

		void AddExtraControl (ExtraControlTableRow extraRow)
		{
			projectConfigurationTable.NRows++;

			projectConfigurationTable.Attach (
				extraRow.Label,
				0,
				1,
				extraRow.Row,
				extraRow.Row + 1,
				AttachOptions.Fill,
				AttachOptions.Fill,
				0,
				0);

			projectConfigurationTable.Attach (
				extraRow.MainWidget,
				1,
				2,
				extraRow.Row,
				extraRow.Row + 1,
				AttachOptions.Fill,
				AttachOptions.Shrink,
				0,
				0);

			if (extraRow.InformationTooltipWidget != null) {
				projectConfigurationTable.Attach (
					extraRow.InformationTooltipWidget,
					2,
					3,
					extraRow.Row,
					extraRow.Row + 1,
					AttachOptions.Fill,
					AttachOptions.Fill,
					0,
					0);
			}

			extraControlRows.Add (extraRow);
		}

		class ExtraControlTableRow
		{
			public uint Row { get; set; }
			public Label Label { get; set; }
			public Widget MainWidget { get; set; }
			public Widget InformationTooltipWidget { get; set; }
			public EventBoxTooltip InformationTooltip { get; set; }
		}
	}
}

