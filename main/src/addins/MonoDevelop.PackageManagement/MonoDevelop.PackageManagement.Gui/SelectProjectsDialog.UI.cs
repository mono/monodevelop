//
// SelectProjectsDialog.UI.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.PackageManagement
{
	partial class SelectProjectsDialog : Dialog
	{
		Label topLabel;
		VBox projectsListView;
		DialogButton okButton;
		List<CheckBox> checkBoxes = new List<CheckBox> ();

		void Build ()
		{
			Title = GettextCatalog.GetString ("Select Projects");
			Width = 420;
			Height = 330;
			Padding = 20;

			var mainVBox = new VBox ();
			Content = mainVBox;

			topLabel = new Label ();
			topLabel.Wrap = WrapMode.Word;
			mainVBox.PackStart (topLabel);

			projectsListView = new VBox ();

			var projectsListScrollView = new ScrollView (projectsListView);
			projectsListScrollView.HorizontalScrollPolicy = ScrollPolicy.Never;
			projectsListScrollView.VerticalScrollPolicy = ScrollPolicy.Automatic;
			projectsListScrollView.BorderVisible = false;
			projectsListScrollView.BackgroundColor = Ide.Gui.Styles.BackgroundColor;
			projectsListScrollView.Content = projectsListView;

			mainVBox.PackStart (projectsListScrollView, true, true);

			var cancelButton = new DialogButton (Command.Cancel);
			Buttons.Add (cancelButton);

			okButton = new DialogButton (Command.Ok);
			okButton.Sensitive = false;
			Buttons.Add (okButton);
		}

		void AddProject (SelectedProjectViewModel project)
		{
			var hbox = new HBox ();
			hbox.Tag = project;

			var checkBox = new CheckBox ();
			checkBox.Label = project.Name;
			checkBox.Tag = project;
			checkBox.Active = project.IsSelected;
			checkBox.Clicked += ProjectCheckBoxClicked;
			hbox.PackStart (checkBox);

			checkBoxes.Add (checkBox);

			projectsListView.PackStart (hbox);
		}

		void ProjectCheckBoxClicked (object sender, EventArgs e)
		{
			var checkBox = (CheckBox)sender;
			var project = (SelectedProjectViewModel)checkBox.Tag;
			project.IsSelected = checkBox.Active;

			UpdateOkButtonSensitivity ();
		}

		void UpdateOkButtonSensitivity ()
		{
			okButton.Sensitive = viewModel.GetSelectedProjects ().Any ();
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			if (disposing) {
				foreach (CheckBox checkBox in checkBoxes) {
					checkBox.Clicked -= ProjectCheckBoxClicked;
				}
			}
		}
	}
}
